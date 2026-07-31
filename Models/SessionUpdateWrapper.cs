using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models;

/// <summary>
/// Params wrapper for session/update notifications.
/// The actual update field is a polymorphic SessionUpdate.
/// </summary>
public record SessionUpdateParams
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("update")]
    public SessionUpdate? Update { get; init; }
}
