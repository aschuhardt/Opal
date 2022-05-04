using Opal.Persistence;

namespace Opal.Tofu;

internal class PersistentCertificateDatabase : InMemoryCertificateDatabase
{
    private readonly FlatKeyValuePairFile<PersistentCertificateDatabase> _flatFile;

    public PersistentCertificateDatabase()
    {
        _flatFile = new FlatKeyValuePairFile<PersistentCertificateDatabase>("remote");
        _flatFile.LoadFromFile(KnownHashesByHost);
    }

    protected override void AfterDatabaseChanged()
    {
        _flatFile.WriteToFile(KnownHashesByHost);
    }
}