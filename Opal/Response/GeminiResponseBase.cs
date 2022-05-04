namespace Opal.Response;

public abstract class GeminiResponseBase : IGeminiResponse
{
    protected GeminiResponseBase(StatusCode status, Uri uri)
    {
        Status = status;
        Uri = uri;
    }

    public StatusCode Status { get; }
    public Uri Uri { get; }

    public override string ToString()
    {
        return $"{(int)Status} {Status.ToString()}";
    }
}