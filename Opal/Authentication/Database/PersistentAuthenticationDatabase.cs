using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Opal.Authentication.Certificate;
using Opal.Persistence;

namespace Opal.Authentication.Database;

internal class PersistentAuthenticationDatabase : InMemoryAuthenticationDatabase
{
    private const string ConfigFileName = "host_certs.json";
    private static readonly object _certLock = new();
    private static readonly object _configLock = new();
    private readonly IDictionary<string, DiskCertificateInfo> _certInfoByHost;

    public PersistentAuthenticationDatabase()
    {
        _certInfoByHost = DeserializeConfiguration();
    }

    private static IDictionary<string, DiskCertificateInfo> DeserializeConfiguration()
    {
        var path = Path.Combine(PersistenceHelper.BuildConfigDirectory(), ConfigFileName);

        if (!File.Exists(path))
            return new Dictionary<string, DiskCertificateInfo>();

        try
        {
            lock (_configLock)
            {
                using var file = File.OpenRead(path);
                return JsonSerializer.Deserialize<IEnumerable<DiskCertificateInfo>>(file)?
                    .ToDictionary(c => c.Host) ?? new Dictionary<string, DiskCertificateInfo>();
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.StackTrace);
            return new Dictionary<string, DiskCertificateInfo>();
        }
    }

    private void SerializeConfiguration()
    {
        var path = Path.Combine(PersistenceHelper.BuildConfigDirectory(), ConfigFileName);

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

    public override void Add(IClientCertificate cert)
    {
        var path = BuildCertificatePath(cert.Certificate);

        _certInfoByHost[cert.Host] = new DiskCertificateInfo
        {
            Path = path,
            Host = cert.Host,
            Name = cert.Name,
            Encrypted = false // TODO
        };

        ExportPem(path, cert.Certificate);
        SerializeConfiguration();

        base.Add(cert);
    }

    private static string BuildCertificatePath(X509Certificate2 certificate)
    {
        return Path.Combine(PersistenceHelper.BuildConfigDirectory("certs"),
            certificate.GetCertHashString(HashAlgorithmName.SHA1) + ".pem");
    }

    public override bool TryGetCertificate(string host, out IClientCertificate certificate)
    {
        // if it's already been loaded, return right away
        if (base.TryGetCertificate(host, out certificate))
            return true;

        // if it's not known, there's nothing we can do
        if (!_certInfoByHost.ContainsKey(host))
            return false;

        var certInfo = _certInfoByHost[host];

        // if it's known but the file doesn't exist then forget it
        if (string.IsNullOrWhiteSpace(certInfo.Path) || !File.Exists(certInfo.Path))
        {
            _certInfoByHost.Remove(host);
            SerializeConfiguration();
            return false;
        }

        try
        {
            // try to load the certificate from disk
            lock (_certLock)
            {
                // workaround for a Windows issue https://github.com/dotnet/runtime/issues/23749
                // TODO when encrypted
                var loaded = new X509Certificate2(X509Certificate2.CreateFromPemFile(certInfo.Path)
                    .Export(X509ContentType.Pkcs12));
                certificate = new ClientCertificate(loaded, host, certInfo.Name);
            }

            base.Add(certificate);
            return true;
        }
        catch
        {
            certificate = null;

            // something went wrong; forget the metadata but leave the file in case
            // we can recover it manually
            _certInfoByHost.Remove(host);
            SerializeConfiguration();
            return false;
        }
    }

    public override void Remove(string host)
    {
        if (StoredCertificates.ContainsKey(host) && !_certInfoByHost.ContainsKey(host))
            return;

        _certInfoByHost.Remove(host);
        SerializeConfiguration();

        var path = _certInfoByHost[host].Path;
        if (File.Exists(path))
        {
            lock (_certLock)
            {
                File.Delete(path);
            }
        }

        base.Remove(host);
    }
}