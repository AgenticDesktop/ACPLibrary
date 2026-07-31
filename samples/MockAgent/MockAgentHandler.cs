using Agentic.ACPLibrary.Agent;
using Agentic.ACPLibrary.Models;
using Agentic.ACPLibrary.Models.Enums;

namespace Agentic.ACPLibrary.Samples.MockAgent;

/// <summary>
/// Deterministic mock agent handler for ACP protocol testing.
/// Replies to prompt text with a streamed <see cref="AgentMessageChunk"/> greeting.
///
/// Supported test directives (first text block, case-insensitive):
/// - "mock:refuse"        → returns StopReason.Refusal without streaming
/// - "mock:sleep=&lt;ms&gt;"    → delays before responding (for cancellation tests)
/// - anything else        → streams a thought chunk + 'Hello! You said "...". What can I do for you?', returns EndTurn
/// </summary>
public sealed class MockAgentHandler : AcpAgentHandlerBase
{
    public override Task<InitializeResponse> HandleInitializeAsync(
        InitializeRequest request, CancellationToken ct = default)
    {
        Console.Error.WriteLine(
            $"[mock-agent] initialize: clientProtocolVersion={request.ProtocolVersion}, " +
            $"client={request.ClientInfo?.Name ?? "unknown"}");

        return Task.FromResult(new InitializeResponse
        {
            ProtocolVersion = Math.Min(request.ProtocolVersion, 1),
            AgentInfo = new ImplementationInfo
            {
                Name = "mock-agent",
                Title = "ACP Mock Agent",
                Version = "0.1.0"
            },
            AgentCapabilities = new AgentCapabilities
            {
                LoadSession = false,
                PromptCapabilities = new PromptCapabilities
                {
                    Image = false,
                    Audio = false,
                    EmbeddedContext = true
                }
            }
        });
    }

    public override Task<SessionNewResponse> HandleNewSessionAsync(
        SessionNewRequest request, CancellationToken ct = default)
    {
        var sessionId = $"mock-{Guid.NewGuid():N}";
        Console.Error.WriteLine($"[mock-agent] session/new: cwd={request.Cwd}, sessionId={sessionId}");

        return Task.FromResult(new SessionNewResponse { SessionId = sessionId });
    }

    public override async Task<SessionPromptResponse> HandlePromptAsync(
        string sessionId, List<ContentBlock> prompt,
        IAcpAgentContext context, CancellationToken ct = default)
    {
        var texts = prompt.OfType<TextContent>().Select(t => t.Text).ToList();
        Console.Error.WriteLine(
            $"[mock-agent] session/prompt: sessionId={sessionId}, blocks={prompt.Count}");

        try
        {
            var directive = texts.FirstOrDefault()?.Trim() ?? string.Empty;

            if (directive.Equals("mock:refuse", StringComparison.OrdinalIgnoreCase))
            {
                return new SessionPromptResponse { StopReason = StopReason.Refusal };
            }

            if (directive.StartsWith("mock:sleep=", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(directive["mock:sleep=".Length..], out var delayMs))
            {
                await Task.Delay(delayMs, ct);
            }

            await context.SendSessionUpdateAsync(sessionId, new AgentThoughtChunk
            {
                SessionId = sessionId,
                Content = new TextContent { Text = $"Received {prompt.Count} content block(s), replying..." }
            }, ct);

            var userInput = string.Join(" ", texts);
            await Task.Delay(20, ct); // Simulate streaming latency
            await context.SendSessionUpdateAsync(sessionId, new AgentMessageChunk
            {
                SessionId = sessionId,
                Content = new TextContent { Text = $"Hello! You said \"{userInput}\". What can I do for you?" }
            }, ct);

            return new SessionPromptResponse { StopReason = StopReason.EndTurn };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine($"[mock-agent] prompt cancelled: sessionId={sessionId}");
            return new SessionPromptResponse { StopReason = StopReason.Cancelled };
        }
    }

    public override Task HandleCancelAsync(string sessionId, CancellationToken ct = default)
    {
        Console.Error.WriteLine($"[mock-agent] session/cancel: sessionId={sessionId}");
        return Task.CompletedTask;
    }
}
