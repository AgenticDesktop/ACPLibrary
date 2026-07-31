using Agentic.ACPLibrary.Client;
using Agentic.ACPLibrary.Models;
using Agentic.ACPLibrary.Protocol;
using Agentic.ACPLibrary.Transport;

// End-to-end smoke test: drive MockAgent through the library's own client stack,
// exactly the way a UI host would.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var mockAgentExe = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "MockAgent", "bin", "Debug", "net10.0", "MockAgent.exe"));

Console.WriteLine($"[test-client] launching agent: {mockAgentExe}");

var transport = new StdioAgentTransport(mockAgentExe, "");
var dispatcher = new JsonRpcDispatcher();
await using IAcpClient client = new AcpClient(transport, dispatcher);

client.SessionUpdated += update =>
{
    switch (update)
    {
        case AgentMessageChunk { Content: TextContent text }:
            Console.WriteLine($"[update] message: {text.Text}");
            break;
        case AgentThoughtChunk { Content: TextContent thought }:
            Console.WriteLine($"[update] thought: {thought.Text}");
            break;
        default:
            Console.WriteLine($"[update] {update.GetType().Name}");
            break;
    }
    return Task.CompletedTask;
};

var info = await client.InitializeAsync();
Console.WriteLine($"[test-client] connected: {info.AgentInfo?.Name} v{info.AgentInfo?.Version}");

var sessionId = await client.CreateSessionAsync(cwd: Directory.GetCurrentDirectory());
Console.WriteLine($"[test-client] session: {sessionId}");

foreach (var text in new[] { "hello", "你好" })
{
    Console.WriteLine($"[test-client] >>> prompt: {text}");
    var response = await client.SendPromptAsync(sessionId,
        [new TextContent { Text = text }]);
    Console.WriteLine($"[test-client] <<< stopReason: {response.StopReason}");
}

await client.ShutdownAsync();
Console.WriteLine("[test-client] done.");
