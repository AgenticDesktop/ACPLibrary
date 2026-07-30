using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ToolCallKind
{
    [JsonStringEnumMemberName("read")]
    Read,
    [JsonStringEnumMemberName("edit")]
    Edit,
    [JsonStringEnumMemberName("delete")]
    Delete,
    [JsonStringEnumMemberName("move")]
    Move,
    [JsonStringEnumMemberName("search")]
    Search,
    [JsonStringEnumMemberName("execute")]
    Execute,
    [JsonStringEnumMemberName("think")]
    Think,
    [JsonStringEnumMemberName("fetch")]
    Fetch,
    [JsonStringEnumMemberName("other")]
    Other
}
