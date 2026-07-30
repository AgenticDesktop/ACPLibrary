using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.JsonRpc;

/// <summary>JSON-RPC 2.0 响应 (有 id + result 或 error)</summary>
public record JsonRpcResponse : JsonRpcMessage
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Result { get; init; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; init; }
}
