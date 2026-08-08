namespace Agentic.ACPLibrary.Registry;

/// <summary>Top-level ACP registry index (https://cdn.agentclientprotocol.com/registry/v1/latest/registry.json).</summary>
public record RegistryIndex
{
    public string Version { get; init; } = string.Empty;

    public List<RegistryAgent> Agents { get; init; } = [];
}

/// <summary>An agent entry in the ACP registry.</summary>
public record RegistryAgent
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? Repository { get; init; }

    public string? Website { get; init; }

    public List<string> Authors { get; init; } = [];

    public string? License { get; init; }

    public string? Icon { get; init; }

    public RegistryDistribution? Distribution { get; init; }
}

/// <summary>Supported distribution channels for an agent.</summary>
public record RegistryDistribution
{
    /// <summary>Platform-specific executables. Key is a platform identifier (e.g. windows-x86_64).</summary>
    public Dictionary<string, BinaryTargetInfo>? Binary { get; init; }

    public PackageDistributionInfo? Npx { get; init; }

    public PackageDistributionInfo? Uvx { get; init; }
}

/// <summary>Binary distribution target for a single platform.</summary>
public record BinaryTargetInfo
{
    public string Archive { get; init; } = string.Empty;

    /// <summary>SHA-256 digest of the archive (64 lowercase or uppercase hex characters).</summary>
    public string? Sha256 { get; init; }

    public string Cmd { get; init; } = string.Empty;

    public List<string> Args { get; init; } = [];

    public Dictionary<string, string> Env { get; init; } = [];
}

/// <summary>Package-based distribution (npx / uvx).</summary>
public record PackageDistributionInfo
{
    public string Package { get; init; } = string.Empty;

    public List<string> Args { get; init; } = [];

    public Dictionary<string, string> Env { get; init; } = [];
}
