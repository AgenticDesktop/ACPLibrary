using System.Text.Json;
using Agentic.ACPLibrary.Client;
using Agentic.ACPLibrary.JsonRpc;
using Agentic.ACPLibrary.Models;
using Agentic.ACPLibrary.Protocol;
using Agentic.ACPLibrary.Transport;

namespace Agentic.ACPLibrary.Tests;

/// <summary>Fake transport that does nothing (satisfies AcpClient constructor).</summary>
internal sealed class FakeTransport : IAgentTransport
{
    public TransportState State => TransportState.Running;
    public event Func<string, Task>? MessageReceived;
    public event Func<Exception, Task>? TransportFaulted;
    public event Func<int, Task>? ProcessExited;

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task SendAsync(string jsonLine, CancellationToken ct = default) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
}

/// <summary>Fake dispatcher that returns a preconfigured response for any request.</summary>
internal sealed class FakeDispatcher : IJsonRpcDispatcher
{
    private readonly JsonRpcResponse _response;

    public FakeDispatcher(JsonRpcResponse response) => _response = response;

    public void Connect(IAgentTransport transport) { }

    public Task<JsonRpcResponse> SendRequestAsync(string method, object? @params, CancellationToken ct = default)
        => Task.FromResult(_response);

    public Task SendNotificationAsync(string method, object? @params, CancellationToken ct = default)
        => Task.CompletedTask;

    public void RegisterRequestHandler(string method, Func<JsonRpcRequest, Task<JsonRpcResponse>> handler) { }
    public void RegisterNotificationHandler(string method, Func<JsonRpcNotification, Task> handler) { }
    public Task DisconnectAsync() => Task.CompletedTask;
}

public class AcpClientErrorHandlingTests
{
    private static AcpClient CreateClientWithErrorResponse(int code, string message)
    {
        var errorResponse = new JsonRpcResponse
        {
            Id = 1,
            Error = new JsonRpcError { Code = code, Message = message }
        };
        return new AcpClient(new FakeTransport(), new FakeDispatcher(errorResponse));
    }

    [Fact]
    public async Task InitializeAsync_ThrowsAcpRpcException_WhenAgentReturnsError()
    {
        var client = CreateClientWithErrorResponse(-32600, "Invalid Request");

        var ex = await Assert.ThrowsAsync<AcpRpcException>(() => client.InitializeAsync());

        Assert.Equal(-32600, ex.ErrorCode);
        Assert.Equal("Invalid Request", ex.ErrorMessage);
        Assert.Contains("-32600", ex.Message);
        Assert.Contains("Invalid Request", ex.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ThrowsAcpRpcException_WhenAgentReturnsError()
    {
        var client = CreateClientWithErrorResponse(-32601, "Method not found");

        var ex = await Assert.ThrowsAsync<AcpRpcException>(() => client.CreateSessionAsync("/tmp"));

        Assert.Equal(-32601, ex.ErrorCode);
        Assert.Equal("Method not found", ex.ErrorMessage);
    }

    [Fact]
    public async Task LoadSessionAsync_ThrowsAcpRpcException_WhenAgentReturnsError()
    {
        var client = CreateClientWithErrorResponse(-32000, "Session not found");

        var ex = await Assert.ThrowsAsync<AcpRpcException>(() => client.LoadSessionAsync("sess-1", "/tmp"));

        Assert.Equal(-32000, ex.ErrorCode);
        Assert.Equal("Session not found", ex.ErrorMessage);
    }

    [Fact]
    public async Task SendPromptAsync_ThrowsAcpRpcException_WhenAgentReturnsError()
    {
        var client = CreateClientWithErrorResponse(-32603, "Internal error");

        var ex = await Assert.ThrowsAsync<AcpRpcException>(
            () => client.SendPromptAsync("sess-1", new List<ContentBlock>()));

        Assert.Equal(-32603, ex.ErrorCode);
        Assert.Equal("Internal error", ex.ErrorMessage);
    }

    [Fact]
    public async Task InitializeAsync_ThrowsProtocolVersionException_WhenVersionMismatch()
    {
        // Return a successful response but with unsupported protocol version
        var initResult = new InitializeResponse
        {
            ProtocolVersion = 99,
            AgentInfo = new ImplementationInfo { Name = "test-agent", Version = "1.0" }
        };
        var successResponse = new JsonRpcResponse
        {
            Id = 1,
            Result = JsonSerializer.SerializeToElement(initResult, Infrastructure.JsonOptions.Default)
        };
        var client = new AcpClient(new FakeTransport(), new FakeDispatcher(successResponse));

        var ex = await Assert.ThrowsAsync<AcpProtocolVersionException>(() => client.InitializeAsync());

        Assert.Equal(1, ex.ClientVersion);
        Assert.Equal(99, ex.AgentVersion);
        Assert.Contains("incompatible", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitializeAsync_Succeeds_WhenVersionMatches()
    {
        var initResult = new InitializeResponse
        {
            ProtocolVersion = 1,
            AgentInfo = new ImplementationInfo { Name = "test-agent", Version = "1.0" }
        };
        var successResponse = new JsonRpcResponse
        {
            Id = 1,
            Result = JsonSerializer.SerializeToElement(initResult, Infrastructure.JsonOptions.Default)
        };
        var client = new AcpClient(new FakeTransport(), new FakeDispatcher(successResponse));

        var result = await client.InitializeAsync();

        Assert.Equal(1, result.ProtocolVersion);
        Assert.True(client.IsInitialized);
    }
}
