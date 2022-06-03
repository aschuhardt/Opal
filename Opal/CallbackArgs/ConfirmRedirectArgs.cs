namespace Opal.CallbackArgs;

public class ConfirmRedirectArgs
{
    public ConfirmRedirectArgs(string uri, bool isPermanent)
    {
        Uri = uri;
        IsPermanent = isPermanent;
        FollowRedirect = true; // follow redirects by default; let the caller opt-out
    }

    public string Uri { get; }
    public bool IsPermanent { get; }

    public bool FollowRedirect { get; set; }
}