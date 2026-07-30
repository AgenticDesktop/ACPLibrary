using System.Text.Json.Serialization;
using Agentic.ACPLibrary.Models.Enums;

namespace Agentic.ACPLibrary.Models;

public record SessionPromptRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("prompt")]
    public List<ContentBlock> Prompt { get; init; } = new();
}

public record SessionPromptResponse
{
    [JsonPropertyName("stopReason")]
    public StopReason StopReason { get; init; }
}
