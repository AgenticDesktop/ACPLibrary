using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StopReason
{
    [JsonStringEnumMemberName("end_turn")]
    EndTurn,
    [JsonStringEnumMemberName("max_tokens")]
    MaxTokens,
    [JsonStringEnumMemberName("max_turn_requests")]
    MaxTurnRequests,
    [JsonStringEnumMemberName("refusal")]
    Refusal,
    [JsonStringEnumMemberName("cancelled")]
    Cancelled
}
