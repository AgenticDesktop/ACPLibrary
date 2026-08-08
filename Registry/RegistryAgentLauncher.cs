namespace Agentic.ACPLibrary.Registry;

/// <summary>
/// Builds the process launch command for an already-installed registry agent.
/// </summary>
public static class RegistryAgentLauncher
{
    /// <summary>
    /// Strips a trailing version specifier from a package spec.
    /// Examples: "agoragentic-mcp@1.3.0" → "agoragentic-mcp"; "@scope/name@1.2.3" → "@scope/name".
    /// </summary>
    public static string StripVersion(string packageSpec)
    {
        var at = packageSpec.LastIndexOf('@');
        return at > 0 ? packageSpec[..at] : packageSpec;
    }

    /// <summary>
    /// Builds the launch command for an installed agent.
    /// npm agents run via <c>npx --no-install</c> (npx.cmd on Windows, since Process.Start with
    /// UseShellExecute=false does not resolve PATH extensions); uv agents run via <c>uvx</c>.
    /// </summary>
    public static (string Command, string Arguments, IReadOnlyDictionary<string, string>? Environment)
        BuildLaunchCommand(InstalledAgentInfo info)
    {
        return info.Kind switch
        {
            AgentInstallKind.Npm => BuildNpx(info),
            AgentInstallKind.Uvx => BuildUvx(info),
            _ => throw new ArgumentOutOfRangeException(nameof(info))
        };
    }

    private static (string Command, string Arguments, IReadOnlyDictionary<string, string>? Environment) BuildNpx(
        InstalledAgentInfo info)
    {
        var dist = info.Agent.Distribution?.Npx;
        var package = StripVersion(dist?.Package ?? info.Agent.Id);
        var arguments = JoinArguments(["--no-install", package, .. dist?.Args ?? []]);
        var env = dist?.Env is { Count: > 0 } e ? e : null;
        return (OperatingSystem.IsWindows() ? "npx.cmd" : "npx", arguments, env);
    }

    private static (string Command, string Arguments, IReadOnlyDictionary<string, string>? Environment) BuildUvx(
        InstalledAgentInfo info)
    {
        var dist = info.Agent.Distribution?.Uvx;
        var package = StripVersion(dist?.Package ?? info.Agent.Id);
        var arguments = JoinArguments([package, .. dist?.Args ?? []]);
        var env = dist?.Env is { Count: > 0 } e ? e : null;
        return ("uvx", arguments, env);
    }

    private static string JoinArguments(IEnumerable<string> tokens)
        => string.Join(" ", tokens.Select(QuoteIfNeeded));

    private static string QuoteIfNeeded(string token)
        => token.Contains(' ') || token.Contains('"')
            ? "\"" + token.Replace("\"", "\\\"") + "\""
            : token;
}
