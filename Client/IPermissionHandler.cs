using Agentic.ACPLibrary.Models;

namespace Agentic.ACPLibrary.Client;

/// <summary>
/// Handles session/request_permission requests from the Agent.
/// Implemented by the UI layer (shows a dialog for user choice).
/// </summary>
public interface IPermissionHandler
{
    /// <summary>
    /// Handles a permission request. Should block until the user makes a choice.
    /// </summary>
    Task<RequestPermissionResponse> HandlePermissionRequestAsync(
        RequestPermissionRequest request, CancellationToken ct = default);
}
