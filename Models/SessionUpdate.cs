using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic.ACPLibrary.Models.Enums;

namespace Agentic.ACPLibrary.Models;

// 以 sessionUpdate 字段作为多态判别符；未识别的更新类型回退为基类，避免 Agent 发送新类型时报错
[JsonPolymorphic(TypeDiscriminatorPropertyName = "sessionUpdate",
    IgnoreUnrecognizedTypeDiscriminators = true,
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(AgentMessageChunk), "agent_message_chunk")]
[JsonDerivedType(typeof(AgentThoughtChunk), "agent_thought_chunk")]
[JsonDerivedType(typeof(UserMessageChunk), "user_message_chunk")]
[JsonDerivedType(typeof(ToolCallNotification), "tool_call")]
[JsonDerivedType(typeof(ToolCallUpdateNotification), "tool_call_update")]
[JsonDerivedType(typeof(PlanUpdate), "plan")]
[JsonDerivedType(typeof(UsageUpdate), "usage_update")]
public class SessionUpdate
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;
}

public class AgentMessageChunk : SessionUpdate
{
    [JsonPropertyName("messageId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageId { get; set; }

    [JsonPropertyName("content")]
    public ContentBlock? Content { get; set; }
}

public class AgentThoughtChunk : SessionUpdate
{
    [JsonPropertyName("content")]
    public ContentBlock? Content { get; set; }
}

public class UserMessageChunk : SessionUpdate
{
    [JsonPropertyName("messageId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageId { get; set; }

    [JsonPropertyName("content")]
    public ContentBlock? Content { get; set; }
}

public class ToolCallNotification : SessionUpdate
{
    [JsonPropertyName("toolCallId")]
    public string ToolCallId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolCallKind? Kind { get; set; }

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolCallStatus? Status { get; set; }
}

public class ToolCallUpdateNotification : SessionUpdate
{
    [JsonPropertyName("toolCallId")]
    public string ToolCallId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolCallStatus? Status { get; set; }

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolCallContentItem>? Content { get; set; }
}

public class ToolCallContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentBlock? Content { get; set; }
}

public class PlanUpdate : SessionUpdate
{
    [JsonPropertyName("entries")]
    public List<PlanEntry> Entries { get; set; } = new();
}

public class PlanEntry
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Priority { get; set; }

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }
}

public class UsageUpdate : SessionUpdate
{
    [JsonPropertyName("used")]
    public long Used { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
