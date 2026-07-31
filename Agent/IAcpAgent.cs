namespace Agentic.ACPLibrary.Agent;

/// <summary>
/// Represents a running ACP Agent that accepts Client connections and handles requests.
/// </summary>
public interface IAcpAgent : IAsyncDisposable
{
    /// <summary>Whether the agent is currently running and accepting connections.</summary>
    bool IsRunning { get; }

    /// <summary>Starts the agent and begins processing Client requests.</summary>
    Task RunAsync(CancellationToken ct = default);

    /// <summary>Stops the agent and cleans up all active sessions.</summary>
    Task StopAsync();
}
