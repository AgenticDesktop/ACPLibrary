using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic.ACPLibrary.JsonRpc;

namespace Agentic.ACPLibrary.Infrastructure;

public static class JsonOptions
{
    private static JsonSerializerOptions? _default;

    public static JsonSerializerOptions Default => _default ??= CreateDefault();

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            // Polymorphic discriminator (e.g. sessionUpdate) is not necessarily the first property of a JSON object
            AllowOutOfOrderMetadataProperties = true
        };
        options.Converters.Add(new JsonRpcMessageConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
