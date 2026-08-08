namespace Agentic.ACPLibrary.Registry;

/// <summary>How an agent is installed on the local machine.</summary>
public enum AgentInstallKind
{
    /// <summary>Globally installed npm package, launched via npx.</summary>
    Npm,

    /// <summary>Tool installed via uv, launched via uvx.</summary>
    Uvx
}

/// <summary>Describes a registry agent that is already installed locally.</summary>
public sealed record InstalledAgentInfo(
    RegistryAgent Agent,
    AgentInstallKind Kind,
    string InstalledVersion,
    bool IsUpToDate);
