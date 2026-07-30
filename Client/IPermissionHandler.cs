using Agentic.ACPLibrary.Models;

namespace Agentic.ACPLibrary.Client;

/// <summary>
/// 处理 Agent 发来的 session/request_permission 请求。
/// 由 UI 层实现（弹出对话框让用户选择）。
/// </summary>
public interface IPermissionHandler
{
    /// <summary>
    /// 处理权限请求。应阻塞直到用户做出选择。
    /// </summary>
    Task<RequestPermissionResponse> HandlePermissionRequestAsync(
        RequestPermissionRequest request, CancellationToken ct = default);
}
