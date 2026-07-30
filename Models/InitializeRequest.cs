using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models;

public record InitializeRequest
{
    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; init; } = 1;

    [JsonPropertyName("clientCapabilities")]
    public ClientCapabilities? ClientCapabilities { get; init; }

    [JsonPropertyName("clientInfo")]
    public ImplementationInfo? ClientInfo { get; init; }
}
