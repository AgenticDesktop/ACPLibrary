using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic.ACPLibrary.Models;

// Uses type field as polymorphic discriminator; unrecognized content types fall back to base type to avoid errors when Agent sends new types
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type",
    IgnoreUnrecognizedTypeDiscriminators = true,
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(TextContent), "text")]
[JsonDerivedType(typeof(ImageContent), "image")]
[JsonDerivedType(typeof(AudioContent), "audio")]
[JsonDerivedType(typeof(ResourceContent), "resource")]
[JsonDerivedType(typeof(ResourceLinkContent), "resource_link")]
public class ContentBlock
{
}

public class TextContent : ContentBlock
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class ImageContent : ContentBlock
{
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;
}

public class AudioContent : ContentBlock
{
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;
}

public class ResourceContent : ContentBlock
{
    [JsonPropertyName("resource")]
    public EmbeddedResource? Resource { get; set; }
}

public class EmbeddedResource
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("blob")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Blob { get; set; }

    [JsonPropertyName("mimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }
}

public class ResourceLinkContent : ContentBlock
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }
}
