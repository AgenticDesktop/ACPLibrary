namespace Agentic.ACPLibrary.Client;

/// <summary>
/// 处理 Agent 发来的 terminal/* 请求。
/// </summary>
public interface ITerminalHandler
{
    /// <summary>创建新终端，返回 terminalId</summary>
    Task<string> CreateTerminalAsync(string command, string? workingDirectory, CancellationToken ct = default);

    /// <summary>获取终端输出</summary>
    Task<string> GetOutputAsync(string terminalId, CancellationToken ct = default);

    /// <summary>等待终端退出</summary>
    Task<int> WaitForExitAsync(string terminalId, CancellationToken ct = default);

    /// <summary>终止终端进程</summary>
    Task KillTerminalAsync(string terminalId, CancellationToken ct = default);

    /// <summary>释放终端资源</summary>
    Task ReleaseTerminalAsync(string terminalId, CancellationToken ct = default);
}
