namespace Opal.Event;

public class ConfirmRedirectEventArgs : EventArgs
{
    public ConfirmRedirectEventArgs(string uri, bool isPermanent)
    {
        Uri = uri;
        IsPermanent = isPermanent;
        FollowRedirect = true; // follow redirects by default; let the caller opt-out
    }

    public string Uri { get; }
    public bool IsPermanent { get; }

    public bool FollowRedirect { get; set; }
}