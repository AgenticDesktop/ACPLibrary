using Agentic.ACPLibrary.Models;

namespace Agentic.ACPLibrary.Agent;

/// <summary>
/// Context interface for Agent to send notifications/requests to Client during prompt handling.
/// </summary>
public interface IAcpAgentContext
{
    /// <summary>Send session/update notification to Client.</summary>
    Task SendSessionUpdateAsync(string sessionId, SessionUpdate update,
        CancellationToken ct = default);

    /// <summary>Request permission from Client.</summary>
    Task<RequestPermissionResponse> RequestPermissionAsync(
        string sessionId, ToolCallInfo toolCall, List<PermissionOption> options,
        CancellationToken ct = default);

    /// <summary>Read a text file on the Client.</summary>
    Task<string> ReadTextFileAsync(string path, CancellationToken ct = default);

    /// <summary>Write a text file on the Client.</summary>
    Task WriteTextFileAsync(string path, string content, CancellationToken ct = default);

    /// <summary>Create a terminal on the Client. Returns terminal ID.</summary>
    Task<string> CreateTerminalAsync(string command, string? workingDirectory,
        CancellationToken ct = default);

    /// <summary>Get output from a terminal on the Client.</summary>
    Task<string> GetTerminalOutputAsync(string terminalId, CancellationToken ct = default);

    /// <summary>Wait for a terminal to exit on the Client. Returns exit code.</summary>
    Task<int> WaitForTerminalExitAsync(string terminalId, CancellationToken ct = default);

    /// <summary>Kill a terminal on the Client.</summary>
    Task KillTerminalAsync(string terminalId, CancellationToken ct = default);

    /// <summary>Release a terminal on the Client.</summary>
    Task ReleaseTerminalAsync(string terminalId, CancellationToken ct = default);
}
