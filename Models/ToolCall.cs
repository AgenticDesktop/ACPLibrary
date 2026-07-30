using System.Text.Json.Serialization;
using Agentic.ACPLibrary.Models.Enums;

namespace Agentic.ACPLibrary.Models;

public record ToolCallInfo
{
    [JsonPropertyName("toolCallId")]
    public string ToolCallId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolCallKind? Kind { get; init; }

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolCallStatus? Status { get; init; }
}
