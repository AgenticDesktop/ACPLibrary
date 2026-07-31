using Agentic.ACPLibrary.Agent;
using Agentic.ACPLibrary.Infrastructure;
using Agentic.ACPLibrary.Samples.MockAgent;
using Microsoft.Extensions.DependencyInjection;

// Mock ACP agent process. Communicates over stdin/stdout (JSON-RPC, one message per line).
// IMPORTANT: all diagnostics go to stderr — stdout is reserved for the JSON-RPC channel.
Console.Error.WriteLine("[mock-agent] starting (stdio transport, protocol v1)...");

var services = new ServiceCollection()
    .AddAcpAgent<MockAgentHandler>()
    .BuildServiceProvider();

await using var agent = services.GetRequiredService<IAcpAgent>();

await agent.RunAsync();

// Keep the process alive until the Client disconnects (stdin EOF) or Ctrl+C.
using var lifetimeCts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    lifetimeCts.Cancel();
};

try
{
    while (agent.IsRunning)
    {
        await Task.Delay(100, lifetimeCts.Token);
    }
}
catch (OperationCanceledException)
{
    // Ctrl+C — fall through to disposal
}

Console.Error.WriteLine("[mock-agent] exiting.");
