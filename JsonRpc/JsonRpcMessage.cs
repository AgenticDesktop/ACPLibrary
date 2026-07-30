using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.JsonRpc;

/// <summary>
/// JSON-RPC 2.0 消息基类型。通过字段存在与否区分请求/响应/通知。
/// </summary>
public record JsonRpcMessage
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";
}
