using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.JsonRpc;

/// <summary>JSON-RPC 2.0 请求 (有 method + id)</summary>
public record JsonRpcRequest : JsonRpcMessage
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Params { get; init; }
}
