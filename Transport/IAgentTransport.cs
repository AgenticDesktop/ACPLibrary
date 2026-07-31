namespace Agentic.ACPLibrary.Transport;

/// <summary>
/// ACP Agent transport abstraction. Supports stdio, mock, and other implementations.
/// </summary>
public interface IAgentTransport
{
    /// <summary>Start the transport (e.g. launch a child process)</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Send a single line of JSON-RPC message</summary>
    Task SendAsync(string jsonLine, CancellationToken cancellationToken = default);

    /// <summary>Raised when a message is received. The parameter is the raw JSON line.</summary>
    event Func<string, Task>? MessageReceived;

    /// <summary>Raised when the transport encounters a fault.</summary>
    event Func<Exception, Task>? TransportFaulted;

    /// <summary>Raised when the underlying process exits. The parameter is the exit code.</summary>
    event Func<int, Task>? ProcessExited;

    /// <summary>Stop the transport (shut down the process)</summary>
    Task StopAsync();

    /// <summary>Current transport state</summary>
    TransportState State { get; }
}

public enum TransportState
{
    Created,
    Starting,
    Running,
    Stopping,
    Stopped,
    Faulted
}
