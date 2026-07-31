using System.Collections.Concurrent;
using System.Text.Json;
using Agentic.ACPLibrary.Infrastructure;
using Agentic.ACPLibrary.JsonRpc;
using Agentic.ACPLibrary.Models;
using Agentic.ACPLibrary.Protocol;
using Agentic.ACPLibrary.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Agentic.ACPLibrary.Agent;

/// <summary>
/// Core ACP Agent implementation. Mirrors <see cref="Client.AcpClient"/> with reversed communication direction.
/// Listens for Client requests and dispatches them to the user-provided <see cref="IAcpAgentHandler"/>.
/// </summary>
public class AcpAgent : IAcpAgent
{
    private readonly IAgentTransport _transport;
    private readonly IJsonRpcDispatcher _dispatcher;
    private readonly IAcpAgentHandler _handler;
    private readonly ILogger<AcpAgent> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeSessions = new();
    private bool _disposed;

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="AcpAgent"/>.
    /// </summary>
    public AcpAgent(
        IAgentTransport transport,
        IJsonRpcDispatcher dispatcher,
        IAcpAgentHandler handler,
        ILogger<AcpAgent>? logger = null)
    {
        _transport = transport;
        _dispatcher = dispatcher;
        _handler = handler;
        _logger = logger ?? NullLogger<AcpAgent>.Instance;
    }

    /// <summary>Starts the transport, registers JSON-RPC handlers, and begins processing Client requests.</summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting ACP agent...");

        await _transport.StartAsync(ct);
        _dispatcher.Connect(_transport);

        // Subscribe to process exit event for Client disconnect detection
        _transport.ProcessExited += async exitCode =>
        {
            _logger.LogWarning("Client process exited with code {ExitCode}", exitCode);
            await StopAsync();
        };

        // Register handler for "initialize" request
        _dispatcher.RegisterRequestHandler("initialize", async request =>
        {
            try
            {
                var initRequest = JsonSerializer.Deserialize<InitializeRequest>(
                    request.Params?.GetRawText() ?? "{}", JsonOptions.Default);

                var response = await _handler.HandleInitializeAsync(initRequest!, ct);

                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = JsonSerializer.SerializeToElement(response, JsonOptions.Default)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling initialize request");
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32603, Message = ex.Message }
                };
            }
        });

        // Register handler for "session/new" request
        _dispatcher.RegisterRequestHandler("session/new", async request =>
        {
            try
            {
                var newSessionRequest = JsonSerializer.Deserialize<SessionNewRequest>(
                    request.Params?.GetRawText() ?? "{}", JsonOptions.Default);

                var response = await _handler.HandleNewSessionAsync(newSessionRequest!, ct);

                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = JsonSerializer.SerializeToElement(response, JsonOptions.Default)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling session/new request");
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32603, Message = ex.Message }
                };
            }
        });

        // Register handler for "session/prompt" request
        _dispatcher.RegisterRequestHandler("session/prompt", async request =>
        {
            try
            {
                var promptRequest = JsonSerializer.Deserialize<SessionPromptRequest>(
                    request.Params?.GetRawText() ?? "{}", JsonOptions.Default);

                var sessionId = promptRequest!.SessionId;
                var context = new AcpAgentContext(_dispatcher);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _activeSessions[sessionId] = cts;

                try
                {
                    var response = await _handler.HandlePromptAsync(
                        sessionId, promptRequest.Prompt, context, cts.Token);

                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = JsonSerializer.SerializeToElement(response, JsonOptions.Default)
                    };
                }
                finally
                {
                    _activeSessions.TryRemove(sessionId, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling session/prompt request");
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32603, Message = ex.Message }
                };
            }
        });

        // Register handler for "session/cancel" notification
        _dispatcher.RegisterNotificationHandler("session/cancel", async notification =>
        {
            try
            {
                var cancelNotification = JsonSerializer.Deserialize<SessionCancelNotification>(
                    notification.Params?.GetRawText() ?? "{}", JsonOptions.Default);

                var sessionId = cancelNotification!.SessionId;

                await _handler.HandleCancelAsync(sessionId, ct);

                if (_activeSessions.TryGetValue(sessionId, out var cts))
                {
                    cts.Cancel();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling session/cancel notification");
            }
        });

        IsRunning = true;
        _logger.LogInformation("ACP agent is running");
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogInformation("Stopping ACP agent...");

        // Cancel all active sessions
        foreach (var kvp in _activeSessions)
        {
            kvp.Value.Cancel();
        }
        _activeSessions.Clear();

        await _dispatcher.DisconnectAsync();
        await _transport.StopAsync();

        IsRunning = false;
        _logger.LogInformation("ACP agent stopped");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Private implementation of <see cref="IAcpAgentContext"/> that sends requests/notifications
    /// back to the Client via the dispatcher.
    /// </summary>
    private sealed class AcpAgentContext : IAcpAgentContext
    {
        private readonly IJsonRpcDispatcher _dispatcher;

        public AcpAgentContext(IJsonRpcDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <inheritdoc />
        public async Task SendSessionUpdateAsync(string sessionId, SessionUpdate update,
            CancellationToken ct = default)
        {
            var updateParams = new SessionUpdateParams { SessionId = sessionId, Update = update };
            await _dispatcher.SendNotificationAsync("session/update", updateParams, ct);
        }

        /// <inheritdoc />
        public async Task<RequestPermissionResponse> RequestPermissionAsync(
            string sessionId, ToolCallInfo toolCall, List<PermissionOption> options,
            CancellationToken ct = default)
        {
            var request = new RequestPermissionRequest
            {
                SessionId = sessionId,
                ToolCall = toolCall,
                Options = options
            };

            var response = await _dispatcher.SendRequestAsync(
                "session/request_permission", request, ct);

            return JsonSerializer.Deserialize<RequestPermissionResponse>(
                response.Result!.Value.GetRawText(), JsonOptions.Default)!;
        }

        /// <inheritdoc />
        public async Task<string> ReadTextFileAsync(string path, CancellationToken ct = default)
        {
            var response = await _dispatcher.SendRequestAsync(
                "fs/read_text_file", new { path }, ct);

            return response.Result!.Value.GetProperty("content").GetString() ?? string.Empty;
        }

        /// <inheritdoc />
        public async Task WriteTextFileAsync(string path, string content, CancellationToken ct = default)
        {
            await _dispatcher.SendRequestAsync(
                "fs/write_text_file", new { path, content }, ct);
        }

        /// <inheritdoc />
        public async Task<string> CreateTerminalAsync(string command, string? workingDirectory,
            CancellationToken ct = default)
        {
            var response = await _dispatcher.SendRequestAsync(
                "terminal/create", new { command, workingDirectory }, ct);

            return response.Result!.Value.GetProperty("terminalId").GetString() ?? string.Empty;
        }

        /// <inheritdoc />
        public async Task<string> GetTerminalOutputAsync(string terminalId, CancellationToken ct = default)
        {
            var response = await _dispatcher.SendRequestAsync(
                "terminal/output", new { terminalId }, ct);

            return response.Result!.Value.GetProperty("output").GetString() ?? string.Empty;
        }

        /// <inheritdoc />
        public async Task<int> WaitForTerminalExitAsync(string terminalId, CancellationToken ct = default)
        {
            var response = await _dispatcher.SendRequestAsync(
                "terminal/wait_for_exit", new { terminalId }, ct);

            return response.Result!.Value.GetProperty("exitCode").GetInt32();
        }

        /// <inheritdoc />
        public async Task KillTerminalAsync(string terminalId, CancellationToken ct = default)
        {
            await _dispatcher.SendRequestAsync(
                "terminal/kill", new { terminalId }, ct);
        }

        /// <inheritdoc />
        public async Task ReleaseTerminalAsync(string terminalId, CancellationToken ct = default)
        {
            await _dispatcher.SendRequestAsync(
                "terminal/release", new { terminalId }, ct);
        }
    }
}
