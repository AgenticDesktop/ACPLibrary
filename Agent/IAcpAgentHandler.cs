using Agentic.ACPLibrary.Models;

namespace Agentic.ACPLibrary.Agent;

/// <summary>
/// User-implemented callback interface for handling Client requests.
/// </summary>
public interface IAcpAgentHandler
{
    /// <summary>Handle Client's initialize request.</summary>
    Task<InitializeResponse> HandleInitializeAsync(
        InitializeRequest request, CancellationToken ct = default);

    /// <summary>Handle Client's session/new request.</summary>
    Task<SessionNewResponse> HandleNewSessionAsync(
        SessionNewRequest request, CancellationToken ct = default);

    /// <summary>Handle Client's session/prompt request.</summary>
    Task<SessionPromptResponse> HandlePromptAsync(
        string sessionId, List<ContentBlock> prompt,
        IAcpAgentContext context, CancellationToken ct = default);

    /// <summary>Handle Client's session/cancel notification.</summary>
    Task HandleCancelAsync(string sessionId, CancellationToken ct = default);
}
