using System.Collections.Concurrent;
using Agentic.ACPLibrary.JsonRpc;

namespace Agentic.ACPLibrary.Protocol;

public class RequestTracker : IRequestTracker
{
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonRpcResponse>> _pending = new();
    private long _nextId;

    public (long id, TaskCompletionSource<JsonRpcResponse> tcs) CreatePendingRequest()
    {
        var id = Interlocked.Increment(ref _nextId);
        var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;
        return (id, tcs);
    }

    public bool TryCompleteRequest(long id, JsonRpcResponse response)
    {
        if (_pending.TryRemove(id, out var tcs))
        {
            if (response.Error is not null)
                tcs.SetException(new JsonRpcException(response.Error.Code, response.Error.Message));
            else
                tcs.SetResult(response);
            return true;
        }
        return false;
    }

    public void CancelAll()
    {
        foreach (var kvp in _pending)
        {
            if (_pending.TryRemove(kvp.Key, out var tcs))
                tcs.SetCanceled();
        }
    }
}

/// <summary>JSON-RPC error exception</summary>
public class JsonRpcException : Exception
{
    public int Code { get; }

    public JsonRpcException(int code, string message) : base(message)
    {
        Code = code;
    }
}
