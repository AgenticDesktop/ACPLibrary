using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models;

public record SessionCancelNotification
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;
}
