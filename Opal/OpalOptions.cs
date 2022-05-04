using Opal.Response;

namespace Opal;

public class OpalOptions
{
    /// <summary>
    ///     Indicates whether or not the client should verify incoming server certificates, both for general integrity as well
    ///     as per TOFU semantics.
    /// </summary>
    public bool VerifyCertificates { get; set; }

    /// <summary>
    ///     Indicates whether or not the client should persist known server certificate metadata to a file on the disk.  If
    ///     this is set to false, recognized certificates hashes will only be stored in-memory and lost when the application
    ///     terminates.
    /// </summary>
    /// <remarks>
    ///     This is done in a thread-safe fashion.
    /// </remarks>
    public bool UsePersistentCertificateDatabase { get; set; }

    /// <summary>
    ///     Indicates whether or not the client should store client certificates on the disk.  If this is set to false, client
    ///     certificates will be lost when the application terminates.
    /// </summary>
    public bool UsePersistentAuthenticationDatabase { get; set; }

    /// <summary>
    ///     Governs how the client should handle redirect responses
    /// </summary>
    public RedirectBehavior RedirectBehavior { get; set; }

    /// <summary>
    ///     The default client behavior, prioritizing security.
    /// </summary>
    public static OpalOptions Default => new()
    {
        UsePersistentCertificateDatabase = true,
        VerifyCertificates = true,
        UsePersistentAuthenticationDatabase = true,
        RedirectBehavior = RedirectBehavior.Follow
    };
}