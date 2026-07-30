using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models;

public record InitializeResponse
{
    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; init; }

    [JsonPropertyName("agentCapabilities")]
    public AgentCapabilities? AgentCapabilities { get; init; }

    [JsonPropertyName("agentInfo")]
    public ImplementationInfo? AgentInfo { get; init; }

    [JsonPropertyName("authMethods")]
    public List<AuthMethod>? AuthMethods { get; init; }
}

public record AuthMethod
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}
