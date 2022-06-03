using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Opal.Authentication.Certificate;
using Opal.Persistence;

namespace Opal.Authentication.Database;

public class PersistentAuthenticationDatabase : InMemoryAuthenticationDatabase
{
    private const int KeyDerivationIterations = 100_000;
    private const string ConfigFileName = "host_certs.json";
    private readonly Dictionary<string, PersistentCertificateInfo> _persistenceInfo;
    private readonly string _rootPath;

    protected PersistentAuthenticationDatabase(string path)
    {
        _persistenceInfo = new Dictionary<string, PersistentCertificateInfo>();
        _rootPath = path ?? PersistenceHelper.BuildConfigDirectory();
        if (!Directory.Exists(_rootPath))
            Directory.CreateDirectory(_rootPath);
    }

    public override IAsyncEnumerable<IClientCertificate> CertificatesAsync => LoadCertificates();

    private async IAsyncEnumerable<IClientCertificate> LoadCertificates()
    {
        foreach (var certInfo in _persistenceInfo.Values)
        {
            if (StoredCertificates.ContainsKey(certInfo.Fingerprint))
            {
                yield return StoredCertificates[certInfo.Fingerprint];
                continue;
            }

            IClientCertificate clientCert;

            try
            {
                // certificate has not yet been loaded from disk, so load it and add it to the cache
                var cert = certInfo.Encrypted
                    ? X509Certificate2.CreateFromEncryptedPemFile(certInfo.Path,
                        await PasswordEntryCallback(certInfo.Fingerprint))
                    : X509Certificate2.CreateFromPemFile(certInfo.Path);
                clientCert = new ClientCertificate(cert);
                StoredCertificates.Add(certInfo.Fingerprint, clientCert);
            }
            catch (Exception e)
            {
                await CertificateFailureCallback($"Failed to load certificate: {e.Message}");
                await RemoveAsync(certInfo.Fingerprint);
                continue;
            }

            yield return clientCert;
        }
    }

    public static async Task<PersistentAuthenticationDatabase> CreateAsync(string path)
    {
        var database = new PersistentAuthenticationDatabase(path);
        foreach (var certInfo in await database.DeserializeConfigurationAsync())
            database._persistenceInfo.Add(certInfo.Fingerprint, certInfo);
        return database;
    }

    protected virtual async Task<IEnumerable<PersistentCertificateInfo>> DeserializeConfigurationAsync()
    {
        var path = Path.Combine(_rootPath, ConfigFileName);

        if (!File.Exists(path))
            return Enumerable.Empty<PersistentCertificateInfo>();

        await using var file = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<IEnumerable<PersistentCertificateInfo>>(file);
    }

    protected virtual async Task SerializeConfigurationAsync()
    {
        var path = Path.Combine(_rootPath, ConfigFileName);

        await using var file = File.Create(path);
        await JsonSerializer.SerializeAsync(file, _persistenceInfo.Values);
    }

    private static void ExportPem(string path, X509Certificate2 certificate)
    {
        using var file = File.CreateText(path);
        file.WriteLine(PemEncoding.Write("CERTIFICATE", certificate.RawData));
        file.WriteLine(PemEncoding.Write("PRIVATE KEY", certificate.GetRSAPrivateKey()?.ExportPkcs8PrivateKey()));
    }

    private static void ExportEncryptedPem(string path, X509Certificate2 certificate, string password)
    {
        using var file = File.CreateText(path);
        file.WriteLine(PemEncoding.Write("CERTIFICATE", certificate.RawData));
        file.WriteLine(PemEncoding.Write("ENCRYPTED PRIVATE KEY",
            certificate.GetRSAPrivateKey()?.ExportEncryptedPkcs8PrivateKey(password,
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256,
                    KeyDerivationIterations))));
    }

    public override async Task AddAsync(IClientCertificate cert, string password)
    {
        var path = BuildCertificatePath(cert.Certificate);

        var certInfo = new PersistentCertificateInfo
        {
            Path = path,
            Fingerprint = cert.Fingerprint,
            Encrypted = !string.IsNullOrEmpty(password)
        };

        if (certInfo.Encrypted)
            ExportEncryptedPem(path, cert.Certificate, password);
        else
            ExportPem(path, cert.Certificate);

        _persistenceInfo[cert.Fingerprint] = certInfo;

        await SerializeConfigurationAsync();

        await base.AddAsync(cert, null);
    }

    protected virtual string BuildCertificatePath(X509Certificate2 certificate)
    {
        return Path.Combine(_rootPath,
            certificate.GetCertHashString(HashAlgorithmName.SHA1) + ".pem");
    }

    public override async Task<IClientCertificate> TryGetCertificateAsync(string fingerprint)
    {
        var certificate = await base.TryGetCertificateAsync(fingerprint);

        // if it's already been loaded, return right away
        if (certificate != null)
            return certificate;

        // if it's not known, there's nothing we can do
        if (!_persistenceInfo.ContainsKey(fingerprint))
        {
            await CertificateFailureCallback("No such certificate exists");
            return null;
        }

        var certInfo = _persistenceInfo[fingerprint];

        // if it's known but the file doesn't exist then forget it
        if (string.IsNullOrWhiteSpace(certInfo.Path) || !File.Exists(certInfo.Path))
        {
            _persistenceInfo.Remove(fingerprint);
            await SerializeConfigurationAsync();
            await CertificateFailureCallback("Certificate file is missing");
            return null;
        }

        try
        {
            // try to load the certificate from disk
            X509Certificate2 loaded;
            if (certInfo.Encrypted)
            {
                var password = await PasswordEntryCallback(certInfo.Fingerprint);
                if (string.IsNullOrEmpty(password))
                {
                    await CertificateFailureCallback("No password provided");
                    return null;
                }

                loaded = X509Certificate2.CreateFromEncryptedPemFile(certInfo.Path, password);
            }
            else
            {
                loaded = X509Certificate2.CreateFromPemFile(certInfo.Path);
            }

            // workaround for a Windows issue https://github.com/dotnet/runtime/issues/23749
            loaded = new X509Certificate2(loaded.Export(X509ContentType.Pkcs12), null as string,
                X509KeyStorageFlags.EphemeralKeySet);

            certificate = new ClientCertificate(loaded);

            await base.AddAsync(certificate, null);

            return certificate;
        }
        catch (CryptographicException e)
        {
            await CertificateFailureCallback($"Decryption failed: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            await CertificateFailureCallback($"An error occurred: {e.Message}");
            return null;
        }
    }

    public override async Task RemoveAsync(string fingerprint)
    {
        if (StoredCertificates.ContainsKey(fingerprint) && _persistenceInfo.ContainsKey(fingerprint))
            return;

        var path = _persistenceInfo[fingerprint].Path;

        _persistenceInfo.Remove(fingerprint);
        await SerializeConfigurationAsync();

        if (File.Exists(path))
            File.Delete(path);

        await base.RemoveAsync(fingerprint);
    }
}