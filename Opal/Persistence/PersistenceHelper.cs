namespace Opal.Persistence;

internal static class PersistenceHelper
{
    public static string BuildConfigDirectory(string subDirectory = null)
    {
#if DEBUG
        var directory = Path.Combine(Environment.CurrentDirectory, Constants.DirectoryName);
#else
        var directory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DirectoryName);
#endif

        if (!string.IsNullOrEmpty(subDirectory))
            directory = Path.Combine(directory, subDirectory);

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        return directory;
    }
}