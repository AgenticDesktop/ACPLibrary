namespace Agentic.ACPLibrary.Client;

/// <summary>
/// Handles terminal/* requests from the Agent.
/// </summary>
public interface ITerminalHandler
{
    /// <summary>Creates a new terminal and returns the terminalId.</summary>
    Task<string> CreateTerminalAsync(string command, string? workingDirectory, CancellationToken ct = default);

    /// <summary>Gets terminal output.</summary>
    Task<string> GetOutputAsync(string terminalId, CancellationToken ct = default);

    /// <summary>Waits for the terminal to exit.</summary>
    Task<int> WaitForExitAsync(string terminalId, CancellationToken ct = default);

    /// <summary>Kills the terminal process.</summary>
    Task KillTerminalAsync(string terminalId, CancellationToken ct = default);

    /// <summary>Releases terminal resources.</summary>
    Task ReleaseTerminalAsync(string terminalId, CancellationToken ct = default);
}
