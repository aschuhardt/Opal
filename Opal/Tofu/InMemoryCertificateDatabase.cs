using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Opal.Tofu;

internal class InMemoryCertificateDatabase : ICertificateDatabase
{
    protected readonly IDictionary<string, string> KnownHashesByHost;

    public InMemoryCertificateDatabase()
    {
        KnownHashesByHost = new ConcurrentDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    }

    public bool IsCertificateValid(string host, X509Certificate certificate,
        out InvalidCertificateReason result)
    {
        var certHost = new X509Certificate2(certificate)
            .GetNameInfo(X509NameType.DnsName, false);

        if (certHost == null)
        {
            // unable to obtain certificate name information
            result = InvalidCertificateReason.MissingInformation;
            return false;
        }

        // ensure that the host matches the certificate
        if (!certHost.Equals(host, StringComparison.InvariantCultureIgnoreCase))
        {
            result = InvalidCertificateReason.NameMismatch;
            return false;
        }

        if (IsExpired(certificate))
        {
            result = InvalidCertificateReason.Expired;
            return false;
        }

        var certHash = certificate.GetCertHashString(HashAlgorithmName.SHA256);

        // first time seeing this; cache a hash of the certificate and indicate success
        if (!KnownHashesByHost.ContainsKey(host))
        {
            KnownHashesByHost.Add(host, certHash);
            result = default;
            AfterDatabaseChanged();
            return true;
        }

        // we have seen this before; verify that it's what we expect
        if (KnownHashesByHost[host] != certHash)
        {
            result = InvalidCertificateReason.TrustedMismatch;
            return false;
        }

        // everything checks out
        result = default;
        return true;
    }

    public void RemoveTrusted(string host)
    {
        if (KnownHashesByHost.ContainsKey(host))
        {
            AfterDatabaseChanged();
            KnownHashesByHost.Remove(host);
        }
    }

    protected virtual void AfterDatabaseChanged()
    {
    }

    private static bool IsExpired(X509Certificate certificate)
    {
        return DateTime.Parse(certificate.GetExpirationDateString()) < DateTime.Now;
    }
}