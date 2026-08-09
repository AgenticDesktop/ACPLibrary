using System.Text.Json;
using Agentic.ACPLibrary.Models;

namespace Agentic.ACPLibrary.Tests;

public class SessionUpdateDeserializationTests
{
    private static SessionUpdate DeserializeUpdate(string updateJson)
    {
        // In the real protocol, the update is wrapped inside session/update params.
        var wrapper = JsonSerializer.Deserialize<SessionUpdateParams>(
            $$"""{"sessionId":"s","update":{{updateJson}}}""");
        return wrapper?.Update
            ?? throw new InvalidOperationException("Failed to deserialize update");
    }

    [Fact]
    public void AvailableCommandsUpdate_DeserializesCorrectly()
    {
        const string json = """
        {
          "sessionUpdate": "available_commands_update",
          "sessionId": "sess-123",
          "availableCommands": [
            { "name": "/help", "description": "Show help" },
            { "name": "/clear", "description": "Clear conversation" },
            { "name": "/summarize", "description": "Summarize chat", "input": { "unstructured": { "hint": "optional topic" } } }
          ]
        }
        """;

        var update = DeserializeUpdate(json);

        var cmdUpdate = Assert.IsType<AvailableCommandsUpdate>(update);
        Assert.Equal("sess-123", cmdUpdate.SessionId);
        Assert.Equal(3, cmdUpdate.AvailableCommands.Count);
        Assert.Equal("/help", cmdUpdate.AvailableCommands[0].Name);
        Assert.Equal("Show help", cmdUpdate.AvailableCommands[0].Description);
        Assert.Equal("/summarize", cmdUpdate.AvailableCommands[2].Name);
        Assert.Equal("optional topic", cmdUpdate.AvailableCommands[2].Input?.Unstructured?.Hint);
    }

    [Fact]
    public void AvailableCommandsUpdate_WithoutDescription_DeserializesWithNullDescription()
    {
        const string json = """
        {
          "sessionUpdate": "available_commands_update",
          "availableCommands": [
            { "name": "/ping" }
          ]
        }
        """;

        var update = DeserializeUpdate(json);
        var cmdUpdate = Assert.IsType<AvailableCommandsUpdate>(update);
        Assert.Single(cmdUpdate.AvailableCommands);
        Assert.Equal("/ping", cmdUpdate.AvailableCommands[0].Name);
        Assert.Null(cmdUpdate.AvailableCommands[0].Description);
    }

    [Fact]
    public void UnknownUpdateType_FallsBackToBaseType()
    {
        const string json = """
        {
          "sessionUpdate": "future_update_type",
          "sessionId": "sess-999"
        }
        """;

        var update = DeserializeUpdate(json);

        Assert.IsType<SessionUpdate>(update);
        Assert.IsNotType<AvailableCommandsUpdate>(update);
        Assert.Equal("sess-999", update.SessionId);
    }

    [Fact]
    public void ExistingUpdateType_AgentMessageChunk_StillDeserializes()
    {
        const string json = """
        {
          "sessionUpdate": "agent_message_chunk",
          "messageId": "msg-1",
          "content": { "type": "text", "text": "hello" }
        }
        """;

        var update = DeserializeUpdate(json);
        Assert.IsType<AgentMessageChunk>(update);
    }
}
