---
kind: error_handling
name: JSON-RPC Error Model and Exception Propagation
category: error_handling
scope:
    - '**'
source_files:
    - JsonRpc/JsonRpcError.cs
    - JsonRpc/JsonRpcResponse.cs
    - Protocol/RequestTracker.cs
    - Protocol/JsonRpcDispatcher.cs
    - Transport/StdioAgentTransport.cs
    - Client/AcpClient.cs
---

This .NET client library implements error handling around JSON-RPC 2.0 communication with an agent process using a layered approach: protocol-level error objects, transport-level fault events, and application-level exceptions.

**Protocol-level errors (JSON-RPC 2.0)**
- `JsonRpcError` (`JsonRpc/JsonRpcError.cs`) is a record with `Code`, `Message`, and optional `Data` fields, serialized per the JSON-RPC 2.0 spec.
- `JsonRpcResponse` (`JsonRpcResponse.cs`) carries either a `Result` or an `Error` field; the `JsonRpcMessageConverter` discriminates between request/response/notification/error by inspecting these fields.
- When a response contains an `Error`, `RequestTracker.TryCompleteRequest` throws a `JsonRpcException` (defined in `Protocol/RequestTracker.cs`) carrying the numeric code and message, which bubbles up through `JsonRpcDispatcher.SendRequestAsync` to callers.

**Transport-level faults**
- `StdioAgentTransport` raises a `TransportFaulted` event when its read loops encounter exceptions (e.g., broken pipe), allowing upper layers to react without crashing the dispatcher.
- Process exit is surfaced via a `ProcessExited` event; the client logs the exit code and re-raises it through `AcpClient.AgentProcessExited`.

**Inbound handler error policy**
- `JsonRpcDispatcher.OnMessageReceivedAsync` wraps deserialization and handler invocation in a try/catch that silently swallows exceptions — malformed messages or handler failures are dropped rather than propagated, keeping the dispatcher resilient.
- For missing pluggable handlers (permission, filesystem, terminal), the client returns a JSON-RPC error with code `-32601` (Method not found) and a descriptive message instead of throwing.

**Outbound validation**
- `JsonRpcDispatcher.SendRequestAsync` and `SendNotificationAsync` throw `InvalidOperationException` if called before `Connect` has been invoked, enforcing correct lifecycle ordering.

**Conventions for developers**
- Treat `JsonRpcException` as the canonical error type returned by all async RPC methods; inspect `Code` and `Message` to distinguish protocol errors from transport faults.
- Use the `TransportFaulted` event for I/O-level failures (process crash, stdio stream errors) rather than relying on exceptions from individual calls.
- When implementing custom request/notification handlers, return a `JsonRpcError` in the response for domain errors instead of throwing, so the dispatcher can serialize them back over the wire.