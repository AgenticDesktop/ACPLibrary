using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models;

/// <summary>
/// session/update 通知的 params 包装。
/// 实际 update 字段是多态的 SessionUpdate。
/// </summary>
public record SessionUpdateParams
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("update")]
    public SessionUpdate? Update { get; init; }
}
