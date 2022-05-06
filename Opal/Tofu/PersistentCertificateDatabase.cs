using System.Text.Json;
using Opal.Persistence;

namespace Opal.Tofu;

internal class PersistentCertificateDatabase : InMemoryCertificateDatabase
{
    private const string ConfigFileName = "remote.json";
    private static readonly object _configLock = new();

    public PersistentCertificateDatabase()
    {
        var path = Path.Combine(PersistenceHelper.BuildConfigDirectory(), ConfigFileName);

        if (!File.Exists(path))
            return;

        try
        {
            lock (_configLock)
            {
                using var file = File.OpenRead(path);
                var hashes = JsonSerializer.Deserialize<IEnumerable<DiskHostCertificateHash>>(file);
                if (hashes == null)
                    return;

                foreach (var hash in hashes)
                    KnownHashesByHost.Add(hash.Host, hash.Hash);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.StackTrace);
        }
    }

    protected override void AfterDatabaseChanged()
    {
        var path = Path.Combine(PersistenceHelper.BuildConfigDirectory(), ConfigFileName);

        try
        {
            lock (_configLock)
            {
                using var file = File.Create(path);
                JsonSerializer.Serialize(file, KnownHashesByHost.Select(h => new DiskHostCertificateHash
                {
                    Host = h.Key,
                    Hash = h.Value
                }));
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.StackTrace);
        }
    }
}