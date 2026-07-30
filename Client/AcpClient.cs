using System.Text.Json;
using Agentic.ACPLibrary.Infrastructure;
using Agentic.ACPLibrary.JsonRpc;
using Agentic.ACPLibrary.Models;
using Agentic.ACPLibrary.Protocol;
using Agentic.ACPLibrary.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Agentic.ACPLibrary.Client;

public class AcpClient
{
    private readonly IAgentTransport _transport;
    private readonly IJsonRpcDispatcher _dispatcher;
    private readonly ILogger<AcpClient> _logger;

    public InitializeResponse? AgentInfo { get; private set; }
    public bool IsInitialized => AgentInfo is not null;

    /// <summary>处理 Agent 发来的权限请求（由 UI 层实现）</summary>
    public IPermissionHandler? PermissionHandler { get; set; }

    /// <summary>处理 Agent 发来的文件系统请求（由 UI 层实现）</summary>
    public IFileSystemHandler? FileSystemHandler { get; set; }

    /// <summary>处理 Agent 发来的 terminal/* 请求（由 UI 层实现）</summary>
    public ITerminalHandler? TerminalHandler { get; set; }

    /// <summary>收到 session/update 通知时触发</summary>
    public event Func<SessionUpdate, Task>? SessionUpdated;

    /// <summary>Agent 进程退出时触发。参数为退出码。</summary>
    public event Func<int, Task>? AgentProcessExited;

    /// <summary>当前会话 ID</summary>
    public string? CurrentSessionId { get; private set; }

    public AcpClient(IAgentTransport transport, IJsonRpcDispatcher dispatcher, ILogger<AcpClient>? logger = null)
    {
        _transport = transport;
        _dispatcher = dispatcher;
        _logger = logger ?? NullLogger<AcpClient>.Instance;
    }

    /// <summary>启动 transport 并完成 initialize 握手</summary>
    public async Task<InitializeResponse> InitializeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing ACP client...");

        await _transport.StartAsync(ct);
        _dispatcher.Connect(_transport);

        // 订阅进程退出事件
        _transport.ProcessExited += async exitCode =>
        {
            _logger.LogWarning("Agent process exited with code {ExitCode}", exitCode);
            if (AgentProcessExited is not null)
                await AgentProcessExited(exitCode);
        };

        // 注册 session/update 通知处理器
        _dispatcher.RegisterNotificationHandler("session/update", async notification =>
        {
            var updateParams = JsonSerializer.Deserialize<SessionUpdateParams>(
                notification.Params?.GetRawText() ?? "{}", JsonOptions.Default);
            if (updateParams?.Update is not null && SessionUpdated is not null)
            {
                await SessionUpdated.Invoke(updateParams.Update);
            }
        });

        // 注册权限请求处理
        _dispatcher.RegisterRequestHandler("session/request_permission", async request =>
        {
            if (PermissionHandler is null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = JsonSerializer.SerializeToElement(
                        new RequestPermissionResponse { Outcome = PermissionOutcome.Cancelled() },
                        JsonOptions.Default)
                };
            }

            var permRequest = JsonSerializer.Deserialize<RequestPermissionRequest>(
                request.Params?.GetRawText() ?? "{}", JsonOptions.Default);

            var response = await PermissionHandler.HandlePermissionRequestAsync(
                permRequest!, CancellationToken.None);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(response, JsonOptions.Default)
            };
        });

        // 注册文件系统读取处理
        _dispatcher.RegisterRequestHandler("fs/read_text_file", async request =>
        {
            if (FileSystemHandler is null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32601, Message = "File system not available" }
                };
            }

            var path = request.Params?.GetProperty("path").GetString() ?? "";
            var content = await FileSystemHandler.ReadTextFileAsync(path);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(new { content }, JsonOptions.Default)
            };
        });

        // 注册文件系统写入处理
        _dispatcher.RegisterRequestHandler("fs/write_text_file", async request =>
        {
            if (FileSystemHandler is null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32601, Message = "File system not available" }
                };
            }

            var path = request.Params?.GetProperty("path").GetString() ?? "";
            var content = request.Params?.GetProperty("content").GetString() ?? "";
            await FileSystemHandler.WriteTextFileAsync(path, content);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonDocument.Parse("{}").RootElement.Clone()
            };
        });

        // 注册 terminal/* 处理
        RegisterTerminalHandlers();

        var initRequest = new InitializeRequest
        {
            ProtocolVersion = 1,
            ClientCapabilities = new ClientCapabilities
            {
                Fs = new FileSystemCapability
                {
                    ReadTextFile = true,
                    WriteTextFile = true
                }
            },
            ClientInfo = new ImplementationInfo
            {
                Name = "agentic-desktop",
                Title = "Agentic Desktop",
                Version = "1.0.0"
            }
        };

        var response = await _dispatcher.SendRequestAsync("initialize", initRequest, ct);
        AgentInfo = JsonSerializer.Deserialize<InitializeResponse>(
            response.Result!.Value.GetRawText(),
            JsonOptions.Default);

        _logger.LogInformation("ACP client initialized. Agent: {AgentName}", AgentInfo?.AgentInfo?.Name);

        // 检查协议版本
        if (AgentInfo?.ProtocolVersion is not null && AgentInfo.ProtocolVersion != 1)
        {
            _logger.LogWarning("Protocol version mismatch: client=1, agent={AgentVersion}", AgentInfo.ProtocolVersion);
        }

        return AgentInfo!;
    }

    /// <summary>创建新会话</summary>
    public async Task<string> CreateSessionAsync(string cwd, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating new session in {Cwd}", cwd);
        var request = new SessionNewRequest { Cwd = cwd };
        var response = await _dispatcher.SendRequestAsync("session/new", request, ct);
        var result = JsonSerializer.Deserialize<SessionNewResponse>(
            response.Result!.Value.GetRawText(), JsonOptions.Default);
        CurrentSessionId = result!.SessionId;
        _logger.LogInformation("Session created: {SessionId}", CurrentSessionId);
        return CurrentSessionId;
    }

    /// <summary>加载已有会话</summary>
    public async Task<string> LoadSessionAsync(string sessionId, string cwd, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading session {SessionId} in {Cwd}", sessionId, cwd);
        var request = new { sessionId, cwd, mcpServers = Array.Empty<object>() };
        await _dispatcher.SendRequestAsync("session/load", request, ct);
        CurrentSessionId = sessionId;
        return CurrentSessionId;
    }

    /// <summary>发送提示并等待响应。流式更新通过 SessionUpdated 事件通知。</summary>
    public async Task<SessionPromptResponse> SendPromptAsync(
        string sessionId, List<ContentBlock> prompt, CancellationToken ct = default)
    {
        _logger.LogDebug("Sending prompt to session {SessionId}", sessionId);
        var request = new SessionPromptRequest { SessionId = sessionId, Prompt = prompt };
        var response = await _dispatcher.SendRequestAsync("session/prompt", request, ct);
        return JsonSerializer.Deserialize<SessionPromptResponse>(
            response.Result!.Value.GetRawText(), JsonOptions.Default)!;
    }

    /// <summary>取消正在进行的提示</summary>
    public async Task CancelSessionAsync(string sessionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Cancelling session {SessionId}", sessionId);
        var notification = new SessionCancelNotification { SessionId = sessionId };
        await _dispatcher.SendNotificationAsync("session/cancel", notification, ct);
    }

    public async Task ShutdownAsync()
    {
        _logger.LogInformation("Shutting down ACP client");
        await _dispatcher.DisconnectAsync();
        await _transport.StopAsync();
    }

    private void RegisterTerminalHandlers()
    {
        // terminal/create
        _dispatcher.RegisterRequestHandler("terminal/create", async request =>
        {
            if (TerminalHandler is null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32601, Message = "Terminal handler not available" }
                };
            }

            var command = request.Params?.GetProperty("command").GetString() ?? "";
            var workingDirectory = request.Params?.TryGetProperty("workingDirectory", out var wd) == true
                ? wd.GetString() : null;

            var terminalId = await TerminalHandler.CreateTerminalAsync(command, workingDirectory);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(new { terminalId }, JsonOptions.Default)
            };
        });

        // terminal/output
        _dispatcher.RegisterRequestHandler("terminal/output", async request =>
        {
            if (TerminalHandler is null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32601, Message = "Terminal handler not available" }
                };
            }

            var terminalId = request.Params?.GetProperty("terminalId").GetString() ?? "";
            var output = await TerminalHandler.GetOutputAsync(terminalId);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(new { output }, JsonOptions.Default)
            };
        });

        // terminal/wait_for_exit
        _dispatcher.RegisterRequestHandler("terminal/wait_for_exit", async request =>
        {
            if (TerminalHandler is null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32601, Message = "Terminal handler not available" }
                };
            }

            var terminalId = request.Params?.GetProperty("terminalId").GetString() ?? "";
            var exitCode = await TerminalHandler.WaitForExitAsync(terminalId);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(new { exitCode }, JsonOptions.Default)
            };
        });

        // terminal/kill
        _dispatcher.RegisterRequestHandler("terminal/kill", async request =>
        {
            if (TerminalHandler is null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32601, Message = "Terminal handler not available" }
                };
            }

            var terminalId = request.Params?.GetProperty("terminalId").GetString() ?? "";
            await TerminalHandler.KillTerminalAsync(terminalId);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonDocument.Parse("{}").RootElement.Clone()
            };
        });

        // terminal/release
        _dispatcher.RegisterRequestHandler("terminal/release", async request =>
        {
            if (TerminalHandler is null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32601, Message = "Terminal handler not available" }
                };
            }

            var terminalId = request.Params?.GetProperty("terminalId").GetString() ?? "";
            await TerminalHandler.ReleaseTerminalAsync(terminalId);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonDocument.Parse("{}").RootElement.Clone()
            };
        });
    }
}
