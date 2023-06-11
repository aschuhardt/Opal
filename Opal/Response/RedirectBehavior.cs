namespace Opal.Response
{
    /// <summary>
    ///     Different methods of handling redirect responses
    /// </summary>
    public enum RedirectBehavior
    {
        /// <summary>
        ///     Automatically follow all redirects unconditionally
        /// </summary>
        Follow,

        /// <summary>
        ///     Confirm each redirect with the caller by invoking the <see cref="IOpalClient.ConfirmRedirectCallback" /> event
        /// </summary>
        Confirm,

        /// <summary>
        ///     Treat redirect responses as any other response and simply return them to the caller
        /// </summary>
        Ignore
    }
}