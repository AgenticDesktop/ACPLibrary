using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.JsonRpc;

/// <summary>JSON-RPC 2.0 notification (has method, no id)</summary>
public record JsonRpcNotification : JsonRpcMessage
{
    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Params { get; init; }
}
