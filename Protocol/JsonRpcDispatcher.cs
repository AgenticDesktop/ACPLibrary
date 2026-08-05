using System.Collections.Concurrent;
using System.Text.Json;
using Agentic.ACPLibrary.Infrastructure;
using Agentic.ACPLibrary.JsonRpc;
using Agentic.ACPLibrary.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Agentic.ACPLibrary.Protocol;

public class JsonRpcDispatcher : IJsonRpcDispatcher
{
    private readonly IRequestTracker _requestTracker;
    private readonly ILogger<JsonRpcDispatcher> _logger;
    private readonly ConcurrentDictionary<string, Func<JsonRpcRequest, Task<JsonRpcResponse>>> _requestHandlers = new();
    private readonly ConcurrentDictionary<string, Func<JsonRpcNotification, Task>> _notificationHandlers = new();
    private IAgentTransport? _transport;

    public JsonRpcDispatcher(IRequestTracker? requestTracker = null, ILogger<JsonRpcDispatcher>? logger = null)
    {
        _requestTracker = requestTracker ?? new RequestTracker();
        _logger = logger ?? NullLogger<JsonRpcDispatcher>.Instance;
    }

    public void Connect(IAgentTransport transport)
    {
        _transport = transport;
        _transport.MessageReceived += OnMessageReceivedAsync;
    }

    public async Task<JsonRpcResponse> SendRequestAsync(string method, object? @params, CancellationToken ct = default)
    {
        if (_transport is null)
            throw new InvalidOperationException("Dispatcher is not connected to a transport.");

        var (id, tcs) = _requestTracker.CreatePendingRequest();

        var request = new JsonRpcRequest
        {
            Id = id,
            Method = method,
            Params = @params is not null
                ? JsonSerializer.SerializeToElement(@params, JsonOptions.Default)
                : null
        };

        var json = JsonSerializer.Serialize(request, JsonOptions.Default);
        await _transport.SendAsync(json, ct);

        return await tcs.Task.WaitAsync(ct);
    }

    public async Task SendNotificationAsync(string method, object? @params, CancellationToken ct = default)
    {
        if (_transport is null)
            throw new InvalidOperationException("Dispatcher is not connected to a transport.");

        var notification = new JsonRpcNotification
        {
            Method = method,
            Params = @params is not null
                ? JsonSerializer.SerializeToElement(@params, JsonOptions.Default)
                : null
        };

        var json = JsonSerializer.Serialize(notification, JsonOptions.Default);
        await _transport.SendAsync(json, ct);
    }

    public void RegisterRequestHandler(string method, Func<JsonRpcRequest, Task<JsonRpcResponse>> handler)
    {
        _requestHandlers[method] = handler;
    }

    public void RegisterNotificationHandler(string method, Func<JsonRpcNotification, Task> handler)
    {
        _notificationHandlers[method] = handler;
    }

    public Task DisconnectAsync()
    {
        if (_transport is not null)
        {
            _transport.MessageReceived -= OnMessageReceivedAsync;
        }
        _requestTracker.CancelAll();
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(string jsonLine)
    {
        try
        {
            var message = JsonSerializer.Deserialize<JsonRpcMessage>(jsonLine, JsonOptions.Default);

            switch (message)
            {
                case JsonRpcResponse response:
                    _requestTracker.TryCompleteRequest(response.Id, response);
                    break;

                case JsonRpcRequest request:
                    await HandleRequestAsync(request);
                    break;

                case JsonRpcNotification notification:
                    if (_notificationHandlers.TryGetValue(notification.Method, out var notifHandler))
                    {
                        await notifHandler(notification);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming JSON-RPC message.");
        }
    }

    private async Task HandleRequestAsync(JsonRpcRequest request)
    {
        if (_transport is null)
            return;

        if (!_requestHandlers.TryGetValue(request.Method, out var handler))
        {
            _logger.LogWarning("No handler registered for method '{Method}'.", request.Method);
            await SendErrorResponseAsync(request.Id, -32601, "Method not found");
            return;
        }

        try
        {
            var resp = await handler(request);
            var respJson = JsonSerializer.Serialize(resp, JsonOptions.Default);
            await _transport.SendAsync(respJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler for method '{Method}' threw an exception.", request.Method);
            await SendErrorResponseAsync(request.Id, -32603, "Internal error");
        }
    }

    private async Task SendErrorResponseAsync(long id, int code, string message)
    {
        if (_transport is null)
            return;

        var errorResponse = new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError { Code = code, Message = message }
        };
        var json = JsonSerializer.Serialize(errorResponse, JsonOptions.Default);
        await _transport.SendAsync(json);
    }
}
