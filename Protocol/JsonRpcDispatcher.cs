using System.Collections.Concurrent;
using System.Text.Json;
using Agentic.ACPLibrary.Infrastructure;
using Agentic.ACPLibrary.JsonRpc;
using Agentic.ACPLibrary.Transport;

namespace Agentic.ACPLibrary.Protocol;

public class JsonRpcDispatcher : IJsonRpcDispatcher
{
    private readonly IRequestTracker _requestTracker;
    private readonly ConcurrentDictionary<string, Func<JsonRpcRequest, Task<JsonRpcResponse>>> _requestHandlers = new();
    private readonly ConcurrentDictionary<string, Func<JsonRpcNotification, Task>> _notificationHandlers = new();
    private IAgentTransport? _transport;

    public JsonRpcDispatcher(IRequestTracker? requestTracker = null)
    {
        _requestTracker = requestTracker ?? new RequestTracker();
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
                    if (_requestHandlers.TryGetValue(request.Method, out var reqHandler))
                    {
                        var resp = await reqHandler(request);
                        if (_transport is not null)
                        {
                            var respJson = JsonSerializer.Serialize(resp, JsonOptions.Default);
                            await _transport.SendAsync(respJson);
                        }
                    }
                    break;

                case JsonRpcNotification notification:
                    if (_notificationHandlers.TryGetValue(notification.Method, out var notifHandler))
                    {
                        await notifHandler(notification);
                    }
                    break;
            }
        }
        catch (Exception)
        {
            // Deserialization failure or handler exception, ignored for now
        }
    }
}
