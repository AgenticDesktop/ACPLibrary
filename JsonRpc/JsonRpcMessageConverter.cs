using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.JsonRpc;

/// <summary>
/// 通过字段存在与否区分 JSON-RPC 消息类型的转换器。
/// </summary>
public class JsonRpcMessageConverter : JsonConverter<JsonRpcMessage>
{
    public override JsonRpcMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var hasMethod = root.TryGetProperty("method", out _);
        var hasId = root.TryGetProperty("id", out _);
        var hasResult = root.TryGetProperty("result", out _);
        var hasError = root.TryGetProperty("error", out _);

        var rawText = root.GetRawText();
        var innerOptions = GetInnerOptions(options);

        if (hasMethod && hasId)
            return JsonSerializer.Deserialize<JsonRpcRequest>(rawText, innerOptions);
        if (hasMethod && !hasId)
            return JsonSerializer.Deserialize<JsonRpcNotification>(rawText, innerOptions);
        if (hasResult || hasError)
            return JsonSerializer.Deserialize<JsonRpcResponse>(rawText, innerOptions);

        return JsonSerializer.Deserialize<JsonRpcMessage>(rawText, innerOptions);
    }

    public override void Write(Utf8JsonWriter writer, JsonRpcMessage value, JsonSerializerOptions options)
    {
        var innerOptions = GetInnerOptions(options);

        switch (value)
        {
            case JsonRpcRequest request:
                JsonSerializer.Serialize(writer, request, innerOptions);
                break;
            case JsonRpcNotification notification:
                JsonSerializer.Serialize(writer, notification, innerOptions);
                break;
            case JsonRpcResponse response:
                JsonSerializer.Serialize(writer, response, innerOptions);
                break;
            default:
                JsonSerializer.Serialize(writer, value, innerOptions);
                break;
        }
    }

    private static JsonSerializerOptions? _innerOptions;

    private static JsonSerializerOptions GetInnerOptions(JsonSerializerOptions options)
    {
        if (_innerOptions is not null) return _innerOptions;

        _innerOptions = new JsonSerializerOptions(options);
        // 移除此 converter 避免递归
        for (int i = _innerOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (_innerOptions.Converters[i] is JsonRpcMessageConverter)
            {
                _innerOptions.Converters.RemoveAt(i);
            }
        }
        return _innerOptions;
    }
}
