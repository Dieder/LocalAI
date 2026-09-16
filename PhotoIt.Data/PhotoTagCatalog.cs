using LiteDB;

namespace PhotoIt.Data;

public sealed class PhotoTagRecord
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public long Size { get; set; }
    public DateTime ModifiedUtc { get; set; }
    public DateTime IndexedUtc { get; set; }
}

public sealed class PhotoTagCatalog : IDisposable
{
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<PhotoTagRecord> _photos;

    public PhotoTagCatalog(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("A database path is required.", nameof(databasePath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _database = new LiteDatabase(databasePath);
        _photos = _database.GetCollection<PhotoTagRecord>("photos");
        _photos.EnsureIndex(photo => photo.Path, unique: true);
    }

    public void Upsert(PhotoTagRecord photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        photo.Id = CreateId(photo.Path);
        _photos.Upsert(photo);
    }

    public PhotoTagRecord? FindByPath(string path)
    {
        return _photos.FindById(CreateId(path));
    }

    public IEnumerable<PhotoTagRecord> GetAll()
    {
        return _photos.FindAll().ToArray();
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    private static string CreateId(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
