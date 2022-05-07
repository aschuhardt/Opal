namespace Opal.Authentication.Database;

public enum CertificateResult
{
    Success,
    Missing,
    NoPassword,
    DecryptionFailure,
    Error
}