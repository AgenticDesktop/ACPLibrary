using Agentic.ACPLibrary.JsonRpc;

namespace Agentic.ACPLibrary.Protocol;

public interface IJsonRpcDispatcher
{
    void Connect(Transport.IAgentTransport transport);
    Task<JsonRpcResponse> SendRequestAsync(string method, object? @params, CancellationToken ct = default);
    Task SendNotificationAsync(string method, object? @params, CancellationToken ct = default);
    void RegisterRequestHandler(string method, Func<JsonRpcRequest, Task<JsonRpcResponse>> handler);
    void RegisterNotificationHandler(string method, Func<JsonRpcNotification, Task> handler);
    Task DisconnectAsync();
}
