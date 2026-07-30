using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models;

public record ClientCapabilities
{
    [JsonPropertyName("fs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileSystemCapability? Fs { get; init; }

    [JsonPropertyName("terminal")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Terminal { get; init; }
}

public record FileSystemCapability
{
    [JsonPropertyName("readTextFile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ReadTextFile { get; init; }

    [JsonPropertyName("writeTextFile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? WriteTextFile { get; init; }
}

public record AgentCapabilities
{
    [JsonPropertyName("loadSession")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LoadSession { get; init; }

    [JsonPropertyName("promptCapabilities")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PromptCapabilities? PromptCapabilities { get; init; }
}

public record PromptCapabilities
{
    [JsonPropertyName("image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Image { get; init; }

    [JsonPropertyName("audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Audio { get; init; }

    [JsonPropertyName("embeddedContext")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EmbeddedContext { get; init; }
}

public record ImplementationInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
}
