using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Opal.Authentication.Certificate;
using Opal.Event;
using Opal.Persistence;

namespace Opal.Authentication.Database;

public class PersistentAuthenticationDatabase : InMemoryAuthenticationDatabase
{
    private const int KeyDerivationIterations = 100_000;
    private const string ConfigFileName = "host_certs.json";
    private static readonly object _certLock = new();
    private static readonly object _configLock = new();
    private readonly IDictionary<string, PersistentCertificateInfo> _certInfoByHost;
    private readonly string _rootPath;

    public PersistentAuthenticationDatabase(string path)
    {
        _certInfoByHost = DeserializeConfiguration();
        _rootPath = path ?? PersistenceHelper.BuildConfigDirectory();
        if (!Directory.Exists(_rootPath))
            Directory.CreateDirectory(_rootPath);
    }

    protected virtual IDictionary<string, PersistentCertificateInfo> DeserializeConfiguration()
    {
        var path = Path.Combine(_rootPath, ConfigFileName);

        if (!File.Exists(path))
            return new Dictionary<string, PersistentCertificateInfo>();

        try
        {
            lock (_configLock)
            {
                using var file = File.OpenRead(path);
                return JsonSerializer.Deserialize<IEnumerable<PersistentCertificateInfo>>(file)?
                    .ToDictionary(c => c.Host) ?? new Dictionary<string, PersistentCertificateInfo>();
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.StackTrace);
            return new Dictionary<string, PersistentCertificateInfo>();
        }
    }

    protected virtual void SerializeConfiguration()
    {
        var path = Path.Combine(_rootPath, ConfigFileName);

        try
        {
            lock (_configLock)
            {
                using var file = File.Create(path);
                JsonSerializer.Serialize(file, _certInfoByHost.Values);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.StackTrace);
        }
    }

    private static void ExportPem(string path, X509Certificate2 certificate)
    {
        lock (_certLock)
        {
            using var file = File.CreateText(path);
            file.WriteLine(PemEncoding.Write("CERTIFICATE", certificate.RawData));
            file.WriteLine(PemEncoding.Write("PRIVATE KEY", certificate.GetRSAPrivateKey()?.ExportPkcs8PrivateKey()));
        }
    }

    private static void ExportEncryptedPem(string path, X509Certificate2 certificate, string password)
    {
        lock (_certLock)
        {
            using var file = File.CreateText(path);
            file.WriteLine(PemEncoding.Write("CERTIFICATE", certificate.RawData));
            file.WriteLine(PemEncoding.Write("ENCRYPTED PRIVATE KEY",
                certificate.GetRSAPrivateKey()?.ExportEncryptedPkcs8PrivateKey(password,
                    new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256,
                        KeyDerivationIterations))));
        }
    }

    public override void Add(IClientCertificate cert, string password)
    {
        var path = BuildCertificatePath(cert.Certificate);

        var certInfo = new PersistentCertificateInfo
        {
            Path = path,
            Host = cert.Host,
            Name = cert.Name,
            Fingerprint = cert.Fingerprint,
            Encrypted = !string.IsNullOrEmpty(password)
        };

        _certInfoByHost[cert.Host] = certInfo;

        if (certInfo.Encrypted)
            ExportEncryptedPem(path, cert.Certificate, password);
        else
            ExportPem(path, cert.Certificate);

        SerializeConfiguration();

        base.Add(cert, null);
    }

    protected virtual string BuildCertificatePath(X509Certificate2 certificate)
    {
        return Path.Combine(_rootPath,
            certificate.GetCertHashString(HashAlgorithmName.SHA1) + ".pem");
    }

    public override CertificateResult TryGetCertificate(string host, out IClientCertificate certificate)
    {
        // if it's already been loaded, return right away
        if (base.TryGetCertificate(host, out certificate) == CertificateResult.Success)
            return CertificateResult.Success;

        // if it's not known, there's nothing we can do
        if (!_certInfoByHost.ContainsKey(host))
            return CertificateResult.Missing;

        var certInfo = _certInfoByHost[host];

        // if it's known but the file doesn't exist then forget it
        if (string.IsNullOrWhiteSpace(certInfo.Path) || !File.Exists(certInfo.Path))
        {
            _certInfoByHost.Remove(host);
            SerializeConfiguration();
            return CertificateResult.Missing;
        }

        try
        {
            // try to load the certificate from disk
            lock (_certLock)
            {
                X509Certificate2 loaded;
                if (certInfo.Encrypted)
                {
                    if (!TryGetPassword(certInfo, out var password))
                        return CertificateResult.NoPassword;

                    loaded = X509Certificate2.CreateFromEncryptedPemFile(certInfo.Path, password);
                }
                else
                    loaded = X509Certificate2.CreateFromPemFile(certInfo.Path);

                // workaround for a Windows issue https://github.com/dotnet/runtime/issues/23749
                loaded = new X509Certificate2(loaded.Export(X509ContentType.Pkcs12), null as string,
                    X509KeyStorageFlags.EphemeralKeySet);

                certificate = new ClientCertificate(loaded, host, certInfo.Name);
            }

            base.Add(certificate, null);

            return CertificateResult.Success;
        }
        catch (CryptographicException)
        {
            certificate = null;
            return CertificateResult.DecryptionFailure;
        }
        catch
        {
            certificate = null;
            return CertificateResult.Error;
        }
    }

    protected virtual bool TryGetPassword(PersistentCertificateInfo certInfo, out string password)
    {
        var args = new CertificatePasswordRequiredEventArgs(certInfo.Host, certInfo.Name, certInfo.Fingerprint);
        CertificatePasswordRequired?.Invoke(this, args);
        password = args.Password;
        return !string.IsNullOrEmpty(password);
    }

    public override void Remove(string host)
    {
        if (StoredCertificates.ContainsKey(host) && !_certInfoByHost.ContainsKey(host))
            return;

        var path = _certInfoByHost[host].Path;

        _certInfoByHost.Remove(host);
        SerializeConfiguration();

        if (File.Exists(path))
        {
            lock (_certLock)
            {
                File.Delete(path);
            }
        }

        base.Remove(host);
    }

    public override event EventHandler<CertificatePasswordRequiredEventArgs> CertificatePasswordRequired;
}