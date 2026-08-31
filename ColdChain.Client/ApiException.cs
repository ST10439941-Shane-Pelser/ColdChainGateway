using ColdChain.Shared.Models;

namespace ColdChain.Client;

/// <summary>
/// Wraps anything the gateway rejected, so the forms can show one readable message
/// instead of dealing with status codes and raw JSON.
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiError? Error { get; }

    public ApiException(string message, int statusCode = 0, ApiError? error = null)
        : base(message)
    {
        StatusCode = statusCode;
        Error = error;
    }
}
