using System.Diagnostics;
using System.Text.Json;

namespace Agentic.ACPLibrary.Registry;

/// <summary>
/// Default <see cref="IInstalledAgentLocator"/>.
/// npm packages are detected by reading the global node_modules directory (via <c>npm root -g</c>);
/// uv tools are detected via <c>uv tool list --json</c>.
/// </summary>
public sealed class InstalledAgentLocator : IInstalledAgentLocator
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(15);

    private sealed record UvToolEntry(string Name, List<UvToolPackage>? Packages);

    private sealed record UvToolPackage(string Name, string Version);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InstalledAgentInfo>> FindInstalledAsync(
        IEnumerable<RegistryAgent> agents, CancellationToken ct = default)
    {
        var agentList = agents.ToList();
        if (agentList.Count == 0) return [];

        var results = new List<InstalledAgentInfo>();

        // Detect npm global packages (only agents with an npx distribution are considered)
        var npmRoot = await RunProcessAsync(GetNpmCommand(), "root -g", ct);
        if (!string.IsNullOrWhiteSpace(npmRoot))
        {
            var root = npmRoot.Trim();
            foreach (var agent in agentList)
            {
                var npx = agent.Distribution?.Npx;
                if (npx is null) continue;

                var package = RegistryAgentLauncher.StripVersion(npx.Package);
                var version = ReadPackageVersion(Path.Combine(root, package));
                if (version is not null)
                {
                    results.Add(new InstalledAgentInfo(agent, AgentInstallKind.Npm, version, version == agent.Version));
                }
            }
        }

        // Detect uv tools (only agents with a uvx distribution are considered)
        var uvJson = await RunProcessAsync("uv", "tool list --json", ct);
        if (!string.IsNullOrWhiteSpace(uvJson))
        {
            var installed = ParseUvTools(uvJson);
            foreach (var agent in agentList)
            {
                var uvx = agent.Distribution?.Uvx;
                if (uvx is null) continue;

                var package = RegistryAgentLauncher.StripVersion(uvx.Package);
                if (installed.TryGetValue(package, out var version))
                {
                    results.Add(new InstalledAgentInfo(agent, AgentInstallKind.Uvx, version, version == agent.Version));
                }
            }
        }

        return results;
    }

    private static string GetNpmCommand() => OperatingSystem.IsWindows() ? "npm.cmd" : "npm";

    private static Dictionary<string, string> ParseUvTools(string json)
    {
        var tools = JsonSerializer.Deserialize<List<UvToolEntry>>(json);
        if (tools is null) return [];

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tool in tools)
        {
            if (tool.Packages is null) continue;
            foreach (var pkg in tool.Packages)
            {
                result.TryAdd(pkg.Name, pkg.Version);
            }
        }
        return result;
    }

    private static string? ReadPackageVersion(string packageJsonPath)
    {
        if (!File.Exists(packageJsonPath)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
            return doc.RootElement.TryGetProperty("version", out var version)
                ? version.GetString()
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Runs a short-lived helper process (npm / uv) and returns its stdout.
    /// Returns null when the executable is missing, times out, or fails — callers treat null as "not available".
    /// </summary>
    private static async Task<string?> RunProcessAsync(string fileName, string arguments, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(ProcessTimeout);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            await process.WaitForExitAsync(timeoutCts.Token);
            return await outputTask;
        }
        catch (Exception)
        {
            // Executable missing, timeout, or process failure — treat as unavailable
            try { process.Kill(entireProcessTree: true); } catch { }
            return null;
        }
    }
}
