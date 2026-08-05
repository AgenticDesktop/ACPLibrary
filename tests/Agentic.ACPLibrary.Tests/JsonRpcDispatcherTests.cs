using System.Text.Json;
using Agentic.ACPLibrary.Infrastructure;
using Agentic.ACPLibrary.JsonRpc;
using Agentic.ACPLibrary.Protocol;
using Agentic.ACPLibrary.Transport;

namespace Agentic.ACPLibrary.Tests;

/// <summary>Transport that records sent messages and allows raising incoming messages.</summary>
internal sealed class CapturingTransport : IAgentTransport
{
    public List<string> SentMessages { get; } = new();

    public TransportState State => TransportState.Running;

    public event Func<string, Task>? MessageReceived;
#pragma warning disable CS0067 // Required by IAgentTransport but unused in tests.
    public event Func<Exception, Task>? TransportFaulted;
    public event Func<int, Task>? ProcessExited;
#pragma warning restore CS0067

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendAsync(string jsonLine, CancellationToken cancellationToken = default)
    {
        SentMessages.Add(jsonLine);
        return Task.CompletedTask;
    }

    public Task StopAsync() => Task.CompletedTask;

    public Task RaiseMessageReceivedAsync(string jsonLine)
        => MessageReceived?.Invoke(jsonLine) ?? Task.CompletedTask;
}

public class JsonRpcDispatcherTests
{
    [Fact]
    public async Task UnknownMethod_RepliesWithMethodNotFoundError()
    {
        var transport = new CapturingTransport();
        var dispatcher = new JsonRpcDispatcher();
        dispatcher.Connect(transport);

        var requestJson = """{"jsonrpc":"2.0","id":42,"method":"unknown/method"}""";
        await transport.RaiseMessageReceivedAsync(requestJson);

        var response = Assert.Single(transport.SentMessages);
        var parsed = JsonSerializer.Deserialize<JsonRpcResponse>(response, JsonOptions.Default);

        Assert.NotNull(parsed);
        Assert.Equal(42, parsed!.Id);
        Assert.NotNull(parsed.Error);
        Assert.Equal(-32601, parsed.Error!.Code);
        Assert.Equal("Method not found", parsed.Error.Message);
    }

    [Fact]
    public async Task HandlerThrowsException_RepliesWithInternalError()
    {
        var transport = new CapturingTransport();
        var dispatcher = new JsonRpcDispatcher();
        dispatcher.RegisterRequestHandler("test/boom", _ => throw new InvalidOperationException("boom"));
        dispatcher.Connect(transport);

        var requestJson = """{"jsonrpc":"2.0","id":7,"method":"test/boom"}""";
        await transport.RaiseMessageReceivedAsync(requestJson);

        var response = Assert.Single(transport.SentMessages);
        var parsed = JsonSerializer.Deserialize<JsonRpcResponse>(response, JsonOptions.Default);

        Assert.NotNull(parsed);
        Assert.Equal(7, parsed!.Id);
        Assert.NotNull(parsed.Error);
        Assert.Equal(-32603, parsed.Error!.Code);
        Assert.Equal("Internal error", parsed.Error.Message);
    }

    [Fact]
    public async Task RegisteredHandler_RepliesWithHandlerResponse()
    {
        var transport = new CapturingTransport();
        var dispatcher = new JsonRpcDispatcher();
        dispatcher.RegisterRequestHandler("test/echo", req => Task.FromResult(new JsonRpcResponse
        {
            Id = req.Id,
            Result = req.Params
        }));
        dispatcher.Connect(transport);

        var requestJson = """{"jsonrpc":"2.0","id":3,"method":"test/echo","params":{"value":1}}""";
        await transport.RaiseMessageReceivedAsync(requestJson);

        var response = Assert.Single(transport.SentMessages);
        var parsed = JsonSerializer.Deserialize<JsonRpcResponse>(response, JsonOptions.Default);

        Assert.NotNull(parsed);
        Assert.Equal(3, parsed!.Id);
        Assert.Null(parsed.Error);
        Assert.NotNull(parsed.Result);
    }
}
