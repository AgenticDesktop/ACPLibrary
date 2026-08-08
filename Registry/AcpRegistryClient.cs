using Agentic.ACPLibrary.Infrastructure;
using System.Text.Json;

namespace Agentic.ACPLibrary.Registry;

/// <summary>Fetches the ACP registry index from the official CDN.</summary>
public interface IAcpRegistryClient
{
    /// <summary>Downloads and parses the registry index.</summary>
    Task<RegistryIndex> FetchIndexAsync(CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IAcpRegistryClient"/> backed by <see cref="HttpClient"/>.
/// </summary>
public sealed class AcpRegistryClient : IAcpRegistryClient
{
    private const string DefaultIndexUrl = "https://cdn.agentclientprotocol.com/registry/v1/latest/registry.json";

    private readonly HttpClient _httpClient;
    private readonly string _indexUrl;

    public AcpRegistryClient(HttpClient? httpClient = null, string indexUrl = DefaultIndexUrl)
    {
        _httpClient = httpClient ?? new HttpClient();
        _indexUrl = indexUrl;
    }

    public async Task<RegistryIndex> FetchIndexAsync(CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(_indexUrl, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<RegistryIndex>(json, JsonOptions.Default)
            ?? new RegistryIndex();
    }
}
