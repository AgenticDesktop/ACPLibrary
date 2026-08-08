using Agentic.ACPLibrary.Registry;

namespace Agentic.ACPLibrary.Tests;

public class RegistryAgentLauncherTests
{
    private static RegistryAgent AgentWith(string id, PackageDistributionInfo? npx, PackageDistributionInfo? uvx) => new()
    {
        Id = id,
        Name = id,
        Version = "1.0.0",
        Distribution = new RegistryDistribution { Npx = npx, Uvx = uvx }
    };

    [Theory]
    [InlineData("agoragentic-mcp@1.3.0", "agoragentic-mcp")]
    [InlineData("@augmentcode/auggie@0.35.0", "@augmentcode/auggie")]
    [InlineData("@scope/name", "@scope/name")]
    [InlineData("plain-package", "plain-package")]
    [InlineData("", "")]
    public void StripVersion_RemovesOnlyTrailingVersion(string spec, string expected)
    {
        Assert.Equal(expected, RegistryAgentLauncher.StripVersion(spec));
    }

    [Fact]
    public void BuildLaunchCommand_Npm_UsesNoInstallAndPreservesArgs()
    {
        var agent = AgentWith("auggie", new PackageDistributionInfo
        {
            Package = "@augmentcode/auggie@0.35.0",
            Args = ["--acp"],
            Env = new Dictionary<string, string> { ["AUGMENT_DISABLE_AUTO_UPDATE"] = "1" }
        }, null);
        var info = new InstalledAgentInfo(agent, AgentInstallKind.Npm, "0.35.0", IsUpToDate: true);

        var (command, arguments, env) = RegistryAgentLauncher.BuildLaunchCommand(info);

        Assert.Equal(OperatingSystem.IsWindows() ? "npx.cmd" : "npx", command);
        Assert.Equal("--no-install @augmentcode/auggie --acp", arguments);
        Assert.NotNull(env);
        Assert.Equal("1", env!["AUGMENT_DISABLE_AUTO_UPDATE"]);
    }

    [Fact]
    public void BuildLaunchCommand_Npm_WithoutArgs_ProducesPlainCommand()
    {
        var agent = AgentWith("plain", new PackageDistributionInfo { Package = "plain@2.0.0" }, null);
        var info = new InstalledAgentInfo(agent, AgentInstallKind.Npm, "2.0.0", IsUpToDate: true);

        var (command, arguments, env) = RegistryAgentLauncher.BuildLaunchCommand(info);

        Assert.Equal(OperatingSystem.IsWindows() ? "npx.cmd" : "npx", command);
        Assert.Equal("--no-install plain", arguments);
        Assert.Null(env);
    }

    [Fact]
    public void BuildLaunchCommand_Uvx_UsesUvxAndPreservesArgs()
    {
        var agent = AgentWith("uvx-agent", null, new PackageDistributionInfo
        {
            Package = "uvx-agent@0.2.0",
            Args = ["serve"]
        });
        var info = new InstalledAgentInfo(agent, AgentInstallKind.Uvx, "0.2.0", IsUpToDate: true);

        var (command, arguments, env) = RegistryAgentLauncher.BuildLaunchCommand(info);

        Assert.Equal("uvx", command);
        Assert.Equal("uvx-agent serve", arguments);
        Assert.Null(env);
    }

    [Fact]
    public void BuildLaunchCommand_FallsBackToAgentId_WhenDistributionMissing()
    {
        // Defensive path: an InstalledAgentInfo should always carry a distribution, but launcher must not crash
        var agent = AgentWith("fallback-id", null, null);
        var info = new InstalledAgentInfo(agent, AgentInstallKind.Npm, "1.0.0", IsUpToDate: true);

        var (command, arguments, _) = RegistryAgentLauncher.BuildLaunchCommand(info);

        Assert.Equal(OperatingSystem.IsWindows() ? "npx.cmd" : "npx", command);
        Assert.Equal("--no-install fallback-id", arguments);
    }

    [Fact]
    public void BuildLaunchCommand_QuotesTokensContainingSpaces()
    {
        var agent = AgentWith("quoted", new PackageDistributionInfo
        {
            Package = "quoted@1.0.0",
            Args = ["--flag", "two words"]
        }, null);
        var info = new InstalledAgentInfo(agent, AgentInstallKind.Npm, "1.0.0", IsUpToDate: true);

        var (_, arguments, _) = RegistryAgentLauncher.BuildLaunchCommand(info);

        Assert.Equal("--no-install quoted --flag \"two words\"", arguments);
    }
}
