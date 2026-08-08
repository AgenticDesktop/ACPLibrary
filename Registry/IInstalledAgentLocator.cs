namespace Agentic.ACPLibrary.Registry;

/// <summary>Detects which registry agents are already installed on the local machine (npm global / uv tools).</summary>
public interface IInstalledAgentLocator
{
    /// <summary>
    /// Returns an <see cref="InstalledAgentInfo"/> for every registry agent that is installed locally.
    /// Missing runtimes (node/npm, uv) are treated as "nothing installed" — never throws.
    /// </summary>
    Task<IReadOnlyList<InstalledAgentInfo>> FindInstalledAsync(IEnumerable<RegistryAgent> agents, CancellationToken ct = default);
}
