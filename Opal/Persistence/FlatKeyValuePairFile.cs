namespace Opal.Persistence;

internal class FlatKeyValuePairFile<T>
{
    private const char Delimiter = '\t';

    // ReSharper disable once StaticMemberInGenericType
    private static readonly object FileLock = new();
    private readonly string _filePath;

    public FlatKeyValuePairFile(string name)
    {
        _filePath = Path.Join(PersistenceHelper.BuildConfigDirectory(), name);
    }

    public void LoadFromFile(IDictionary<string, string> dataSource)
    {
        if (!File.Exists(_filePath))
            return;

        lock (FileLock)
        {
            using var file = File.OpenRead(_filePath);
            using var reader = new StreamReader(file);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (line == null || line.StartsWith('#'))
                    continue;

                var parts = line.Split(Delimiter);
                if (parts.Length != 2)
                    continue;

                dataSource.Add(parts[0], parts[1]);
            }
        }
    }

    public void WriteToFile(IDictionary<string, string> dataSource)
    {
        lock (FileLock)
        {
            using var file = File.CreateText(_filePath);
            file.WriteLine($"# Updated {DateTime.Now:G}");
            file.WriteLine();
            foreach (var (key, val) in dataSource)
                file.WriteLine($"{key}{Delimiter}{val}");
        }
    }
}