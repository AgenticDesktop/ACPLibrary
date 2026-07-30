using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ToolCallStatus
{
    [JsonStringEnumMemberName("pending")]
    Pending,
    [JsonStringEnumMemberName("in_progress")]
    InProgress,
    [JsonStringEnumMemberName("completed")]
    Completed,
    [JsonStringEnumMemberName("failed")]
    Failed
}
