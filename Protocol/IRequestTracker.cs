using Agentic.ACPLibrary.JsonRpc;

namespace Agentic.ACPLibrary.Protocol;

public interface IRequestTracker
{
    (long id, TaskCompletionSource<JsonRpcResponse> tcs) CreatePendingRequest();
    bool TryCompleteRequest(long id, JsonRpcResponse response);
    void CancelAll();
}
