using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models;

public record SessionNewRequest
{
    [JsonPropertyName("cwd")]
    public string Cwd { get; init; } = string.Empty;

    // ACP spec requires mcpServers as a required array; send empty array when omitted (agents like copilot validate and return Invalid params)
    [JsonPropertyName("mcpServers")]
    public List<McpServerConfig> McpServers { get; init; } = new();
}

public record SessionNewResponse
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;
}

public record McpServerConfig
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("command")]
    public string Command { get; init; } = string.Empty;

    [JsonPropertyName("args")]
    public List<string> Args { get; init; } = new();

    [JsonPropertyName("env")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpEnvVariable>? Env { get; init; }
}

public record McpEnvVariable
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;
}
