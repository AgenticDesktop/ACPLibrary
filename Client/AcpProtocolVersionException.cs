namespace Agentic.ACPLibrary.Client;

/// <summary>
/// Thrown when the Agent's protocol version is incompatible with the client.
/// </summary>
public class AcpProtocolVersionException : Exception
{
    /// <summary>Protocol version supported by the client.</summary>
    public int ClientVersion { get; }

    /// <summary>Protocol version reported by the Agent.</summary>
    public int AgentVersion { get; }

    public AcpProtocolVersionException(int clientVersion, int agentVersion)
        : base($"Protocol version incompatible: client supports {clientVersion}, agent requires {agentVersion}. " +
               "Please upgrade the client or use a compatible agent.")
    {
        ClientVersion = clientVersion;
        AgentVersion = agentVersion;
    }
}
