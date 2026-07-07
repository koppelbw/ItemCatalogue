namespace Application.AnthropicPorts;

// Raised by the Infrastructure adapter when the Anthropic API returns a non-success status.
// ErrorType carries the API's machine-readable error type (e.g. "rate_limit_error",
// "authentication_error") so callers and logs can distinguish failure modes.
public sealed class AnthropicApiException(int statusCode, string errorType, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public string ErrorType { get; } = errorType;
}
