using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Opal.Persistence;

namespace Opal.Authentication;

internal class PersistentAuthenticationDatabase : InMemoryAuthenticationDatabase
{
    private static readonly object _fileLock = new();
    private readonly FlatKeyValuePairFile<PersistentAuthenticationDatabase> _flatFile;
    private readonly IDictionary<string, string> _pathsByHost;

    public PersistentAuthenticationDatabase()
    {
        _pathsByHost = new ConcurrentDictionary<string, string>();
        _flatFile = new FlatKeyValuePairFile<PersistentAuthenticationDatabase>("host_certs");
        _flatFile.LoadFromFile(_pathsByHost);
    }

    public override void Add(string host, X509Certificate2 certificate)
    {
        var fileContents = new StringBuilder();
        fileContents.Append(PemEncoding.Write("CERTIFICATE", certificate.RawData)).AppendLine();
        fileContents.Append(PemEncoding.Write("PRIVATE KEY",
            certificate.GetRSAPrivateKey()?.ExportPkcs8PrivateKey()));

        lock (_fileLock)
        {
            File.WriteAllText(BuildCertificatePath(certificate), fileContents.ToString());
        }

        _pathsByHost[host] = BuildCertificatePath(certificate);
        _flatFile.WriteToFile(_pathsByHost);
        base.Add(host, certificate);
    }

    private static string BuildCertificatePath(X509Certificate2 certificate)
    {
        return Path.Combine(PersistenceHelper.BuildConfigDirectory("certs"),
            certificate.GetCertHashString(HashAlgorithmName.SHA1) + ".pem");
    }

    public override bool TryGetCertificate(string host, out X509Certificate2 certificate)
    {
        var found = base.TryGetCertificate(host, out certificate);

        if (!found && _pathsByHost.ContainsKey(host))
        {
            var filePath = _pathsByHost[host];

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                _pathsByHost.Remove(host);
                _flatFile.WriteToFile(_pathsByHost);
                return false;
            }

            try
            {
                // workaround for a Windows issue https://github.com/dotnet/runtime/issues/23749
                certificate = new X509Certificate2(X509Certificate2.CreateFromPemFile(filePath)
                    .Export(X509ContentType.Pkcs12));

                base.Add(host, certificate);

                return true;
            }
            catch
            {
                lock (_fileLock)
                {
                    File.Delete(filePath);
                }

                _pathsByHost.Remove(host);
                _flatFile.WriteToFile(_pathsByHost);
                return false;
            }
        }

        return found;
    }

    public override void Remove(string host)
    {
        if (Certificates.ContainsKey(host))
        {
            var filePath = BuildCertificatePath(Certificates[host]);
            if (File.Exists(filePath))
                lock (_fileLock)
                {
                    File.Delete(filePath);
                }

            if (_pathsByHost.ContainsKey(host))
            {
                _pathsByHost.Remove(host);
                _flatFile.WriteToFile(_pathsByHost);
            }

            base.Remove(host);
        }
    }
}