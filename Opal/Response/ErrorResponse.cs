namespace Opal.Response;

public class ErrorResponse : GeminiResponseBase
{
    public ErrorResponse(Uri uri, StatusCode status, string message) : base(status, uri)
    {
        Message = message;
    }

    public string Message { get; }

    public override string ToString()
    {
        return $"{base.ToString()} \"{Message}\"";
    }
}