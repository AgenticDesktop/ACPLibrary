using Agentic.ACPLibrary.Models;

namespace Agentic.ACPLibrary.Agent;

/// <summary>
/// Convenience abstract base class for <see cref="IAcpAgentHandler"/> with default implementations.
/// Subclasses only need to override <see cref="HandlePromptAsync"/> at minimum.
/// </summary>
public abstract class AcpAgentHandlerBase : IAcpAgentHandler
{
    /// <inheritdoc />
    public virtual Task<InitializeResponse> HandleInitializeAsync(
        InitializeRequest request, CancellationToken ct = default)
        => Task.FromResult(new InitializeResponse { ProtocolVersion = 1 });

    /// <inheritdoc />
    public virtual Task<SessionNewResponse> HandleNewSessionAsync(
        SessionNewRequest request, CancellationToken ct = default)
        => Task.FromResult(new SessionNewResponse { SessionId = Guid.NewGuid().ToString() });

    /// <inheritdoc />
    public abstract Task<SessionPromptResponse> HandlePromptAsync(
        string sessionId, List<ContentBlock> prompt,
        IAcpAgentContext context, CancellationToken ct = default);

    /// <inheritdoc />
    public virtual Task HandleCancelAsync(string sessionId, CancellationToken ct = default)
        => Task.CompletedTask;
}
