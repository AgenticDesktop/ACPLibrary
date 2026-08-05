namespace Agentic.ACPLibrary.Client;

/// <summary>
/// Thrown when the Agent returns a JSON-RPC error response.
/// Carries the protocol error code and message.
/// </summary>
public class AcpRpcException : Exception
{
    /// <summary>JSON-RPC error code returned by the Agent.</summary>
    public int ErrorCode { get; }

    /// <summary>JSON-RPC error message returned by the Agent.</summary>
    public string ErrorMessage { get; }

    public AcpRpcException(int errorCode, string errorMessage)
        : base($"ACP RPC error {errorCode}: {errorMessage}")
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public AcpRpcException(int errorCode, string errorMessage, Exception? innerException)
        : base($"ACP RPC error {errorCode}: {errorMessage}", innerException)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}
