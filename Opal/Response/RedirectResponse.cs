namespace Opal.Response;

public class RedirectResponse : GeminiResponseBase
{
    public RedirectResponse(Uri uri, bool isPermanent, string redirectTo) : base(isPermanent
        ? StatusCode.PermanentRedirect
        : StatusCode.TemporaryRedirect, uri)
    {
        IsPermanent = isPermanent;
        RedirectTo = redirectTo;
    }

    public bool IsPermanent { get; }
    public string RedirectTo { get; }
}