namespace Agentic.ACPLibrary.Transport;

/// <summary>
/// ACP Agent 传输层抽象。支持 stdio、mock 等实现。
/// </summary>
public interface IAgentTransport
{
    /// <summary>启动传输（如启动子进程）</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>发送一行 JSON-RPC 消息</summary>
    Task SendAsync(string jsonLine, CancellationToken cancellationToken = default);

    /// <summary>收到消息时触发。参数为原始 JSON 行。</summary>
    event Func<string, Task>? MessageReceived;

    /// <summary>传输层发生故障时触发。</summary>
    event Func<Exception, Task>? TransportFaulted;

    /// <summary>底层进程退出时触发。参数为退出码。</summary>
    event Func<int, Task>? ProcessExited;

    /// <summary>停止传输（关闭进程）</summary>
    Task StopAsync();

    /// <summary>当前传输状态</summary>
    TransportState State { get; }
}

public enum TransportState
{
    Created,
    Starting,
    Running,
    Stopping,
    Stopped,
    Faulted
}
