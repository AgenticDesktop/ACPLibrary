using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models;

public record RequestPermissionRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("toolCall")]
    public ToolCallInfo? ToolCall { get; init; }

    [JsonPropertyName("options")]
    public List<PermissionOption> Options { get; init; } = new();
}

public record PermissionOption
{
    [JsonPropertyName("optionId")]
    public string OptionId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;
}

public record RequestPermissionResponse
{
    [JsonPropertyName("outcome")]
    public PermissionOutcome Outcome { get; init; } = new();
}

public record PermissionOutcome
{
    [JsonPropertyName("outcome")]
    public string OutcomeType { get; init; } = "cancelled";

    [JsonPropertyName("optionId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OptionId { get; init; }

    public static PermissionOutcome Cancelled() => new() { OutcomeType = "cancelled" };
    public static PermissionOutcome Selected(string optionId) => new() { OutcomeType = "selected", OptionId = optionId };
}
