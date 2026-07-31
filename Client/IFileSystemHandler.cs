namespace Agentic.ACPLibrary.Client;

/// <summary>
/// Handles fs/* file system requests from the Agent.
/// </summary>
public interface IFileSystemHandler
{
    /// <summary>Reads text file content.</summary>
    Task<string> ReadTextFileAsync(string path, CancellationToken ct = default);

    /// <summary>Writes text file content.</summary>
    Task WriteTextFileAsync(string path, string content, CancellationToken ct = default);
}
