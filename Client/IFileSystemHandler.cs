namespace Agentic.ACPLibrary.Client;

/// <summary>
/// 处理 Agent 发来的 fs/* 文件系统请求。
/// </summary>
public interface IFileSystemHandler
{
    /// <summary>读取文本文件内容</summary>
    Task<string> ReadTextFileAsync(string path, CancellationToken ct = default);

    /// <summary>写入文本文件内容</summary>
    Task WriteTextFileAsync(string path, string content, CancellationToken ct = default);
}
