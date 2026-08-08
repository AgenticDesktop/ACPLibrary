using System.Text.Json;
using Agentic.ACPLibrary.Infrastructure;
using Agentic.ACPLibrary.Registry;

namespace Agentic.ACPLibrary.Tests;

public class RegistryModelTests
{
    private static readonly string RealIndexSample = """
        {
          "version": "1.0.0",
          "agents": [
            {
              "id": "amp-acp",
              "name": "Amp",
              "version": "0.9.0",
              "description": "ACP wrapper for Amp - the frontier coding agent",
              "repository": "https://github.com/tao12345666333/amp-acp",
              "authors": ["tao12345666333"],
              "license": "Apache-2.0",
              "icon": "https://cdn.agentclientprotocol.com/registry/v1/latest/amp-acp.svg",
              "distribution": {
                "binary": {
                  "windows-x86_64": {
                    "archive": "https://github.com/tao12345666333/amp-acp/releases/download/v0.9.0/amp-acp-windows-x86_64.zip",
                    "cmd": "amp-acp.exe",
                    "sha256": "3b2c3d14d703fcf9572da9733e4941703a7744bd37ec4aaa75421d6002c0157b"
                  },
                  "darwin-aarch64": {
                    "archive": "https://github.com/tao12345666333/amp-acp/releases/download/v0.9.0/amp-acp-darwin-aarch64.tar.gz",
                    "cmd": "./amp-acp",
                    "sha256": "240a1a464f2a400ae51e9613b7f52b2abb6e7a29759001e9185291325671ccf1"
                  }
                }
              }
            },
            {
              "id": "auggie",
              "name": "Auggie CLI",
              "version": "0.35.0",
              "description": "Augment Code's powerful software agent",
              "repository": "https://github.com/augmentcode/auggie",
              "website": "https://www.augmentcode.com/",
              "authors": ["Augment Code <support@augmentcode.com>"],
              "license": "proprietary",
              "icon": "https://cdn.agentclientprotocol.com/registry/v1/latest/auggie.svg",
              "distribution": {
                "npx": {
                  "package": "@augmentcode/auggie@0.35.0",
                  "args": ["--acp"],
                  "env": { "AUGMENT_DISABLE_AUTO_UPDATE": "1" }
                }
              }
            },
            {
              "id": "uvx-agent",
              "name": "Uvx Agent",
              "version": "0.2.0",
              "description": "Agent installed via uv",
              "distribution": {
                "uvx": {
                  "package": "uvx-agent@0.2.0",
                  "args": ["serve"]
                }
              }
            }
          ]
        }
        """;

    [Fact]
    public void Deserialize_RealIndexSample_ParsesAllFields()
    {
        var index = JsonSerializer.Deserialize<RegistryIndex>(RealIndexSample, JsonOptions.Default)!;

        Assert.Equal("1.0.0", index.Version);
        Assert.Equal(3, index.Agents.Count);
    }

    [Fact]
    public void Deserialize_BinaryDistribution_ParsesPlatformTargets()
    {
        var index = JsonSerializer.Deserialize<RegistryIndex>(RealIndexSample, JsonOptions.Default)!;
        var amp = index.Agents[0];

        Assert.Equal("amp-acp", amp.Id);
        Assert.Equal("Amp", amp.Name);
        Assert.Equal("Apache-2.0", amp.License);
        Assert.Single(amp.Authors);

        var binary = amp.Distribution!.Binary!;
        Assert.Equal(2, binary.Count);
        Assert.True(binary.ContainsKey("windows-x86_64"));
        Assert.True(binary.ContainsKey("darwin-aarch64"));

        var win = binary["windows-x86_64"];
        Assert.Equal("amp-acp.exe", win.Cmd);
        Assert.EndsWith(".zip", win.Archive);
        Assert.Equal("3b2c3d14d703fcf9572da9733e4941703a7744bd37ec4aaa75421d6002c0157b", win.Sha256);
        Assert.Empty(win.Args);
        Assert.Empty(win.Env);

        var mac = binary["darwin-aarch64"];
        Assert.Equal("./amp-acp", mac.Cmd);
        Assert.EndsWith(".tar.gz", mac.Archive);
    }

    [Fact]
    public void Deserialize_NpxDistribution_ParsesPackageArgsAndEnv()
    {
        var index = JsonSerializer.Deserialize<RegistryIndex>(RealIndexSample, JsonOptions.Default)!;
        var auggie = index.Agents[1];

        Assert.Equal("Auggie CLI", auggie.Name);
        Assert.Equal("https://www.augmentcode.com/", auggie.Website);
        Assert.Equal("proprietary", auggie.License);

        var npx = auggie.Distribution!.Npx!;
        Assert.Equal("@augmentcode/auggie@0.35.0", npx.Package);
        Assert.Equal(["--acp"], npx.Args);
        Assert.Equal("1", npx.Env["AUGMENT_DISABLE_AUTO_UPDATE"]);
    }

    [Fact]
    public void Deserialize_UvxDistribution_ParsesPackageAndArgs()
    {
        var index = JsonSerializer.Deserialize<RegistryIndex>(RealIndexSample, JsonOptions.Default)!;
        var agent = index.Agents[2];

        var uvx = agent.Distribution!.Uvx!;
        Assert.Equal("uvx-agent@0.2.0", uvx.Package);
        Assert.Equal(["serve"], uvx.Args);
        Assert.Null(agent.Distribution.Binary);
        Assert.Null(agent.Distribution.Npx);
    }
}
