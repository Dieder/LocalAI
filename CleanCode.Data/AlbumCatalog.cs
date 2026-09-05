using LiteDB;

namespace CleanCode.Data;

public class Album
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public string ImageFolderName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
}

public sealed class AlbumPhoto
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string AlbumId { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long Length { get; set; }
    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public DateTime? IndexedUtc { get; set; }
}

public sealed class AlbumCatalog : IDisposable
{
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<Album> _albums;
    private readonly ILiteCollection<AlbumPhoto> _photos;

    public AlbumCatalog(string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        _database = new LiteDatabase(databasePath);
        _albums = _database.GetCollection<Album>("albums");
        _photos = _database.GetCollection<AlbumPhoto>("albumPhotos");
        _albums.EnsureIndex(album => album.ImageFolderName, unique: true);
        _albums.EnsureIndex(album => album.Name);
        _photos.EnsureIndex(photo => photo.AlbumId);
    }

    public IReadOnlyList<Album> List()
    {
        return _albums.Query()
            .OrderByDescending(album => album.UpdatedUtc)
            .ToList();
    }

    public Album? Get(string id)
    {
        return _albums.FindById(id);
    }

    public bool ImageFolderNameExists(string imageFolderName, string? excludingId = null)
    {
        var normalizedName = NormalizeFolderName(imageFolderName);
        return _albums.FindAll().Any(album =>
            string.Equals(album.ImageFolderName, normalizedName, StringComparison.Ordinal) &&
            !string.Equals(album.Id, excludingId, StringComparison.OrdinalIgnoreCase));
    }

    public void Create(Album album)
    {
        ArgumentNullException.ThrowIfNull(album);
        album.Id = Guid.NewGuid().ToString("N");
        album.CreatedUtc = DateTime.UtcNow;
        album.UpdatedUtc = album.CreatedUtc;
        album.ImageFolderName = $"ALBUM-{album.Id.ToUpperInvariant()}";
        _albums.Insert(album);
    }

    public bool Update(Album album)
    {
        ArgumentNullException.ThrowIfNull(album);
        album.ImageFolderName = NormalizeFolderName(album.ImageFolderName);
        album.UpdatedUtc = DateTime.UtcNow;
        return _albums.Update(album);
    }

    public bool Delete(string id)
    {
        _photos.DeleteMany(photo => photo.AlbumId == id);
        return _albums.Delete(id);
    }

    public IReadOnlyList<AlbumPhoto> ListPhotos(string albumId)
    {
        var photos = _photos.Find(photo => photo.AlbumId == albumId)
            .OrderByDescending(photo => photo.UploadedUtc)
            .ToList();

        foreach (var photo in photos)
            NormalizeLegacyAnalysis(photo);

        return photos;
    }

    public void AddPhoto(AlbumPhoto photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        _photos.Insert(photo);
    }

    public AlbumPhoto? GetPhoto(string albumId, string photoId)
    {
        var photo = _photos.FindOne(photo => photo.AlbumId == albumId && photo.Id == photoId);
        if (photo is not null)
            NormalizeLegacyAnalysis(photo);
        return photo;
    }

    public bool DeletePhoto(string id)
    {
        return _photos.Delete(id);
    }

    public bool UpdatePhoto(AlbumPhoto photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        return _photos.Update(photo);
    }

    public void Touch(string albumId)
    {
        var album = Get(albumId);
        if (album is null)
            return;

        album.UpdatedUtc = DateTime.UtcNow;
        _albums.Update(album);
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    private static string NormalizeFolderName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private void NormalizeLegacyAnalysis(AlbumPhoto photo)
    {
        if (photo.Tags.Count > 0 || string.IsNullOrWhiteSpace(photo.Description))
            return;

        var start = photo.Description.IndexOf('{');
        var end = photo.Description.LastIndexOf('}');
        if (start < 0 || end <= start)
            return;

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(photo.Description[start..(end + 1)]);
            var root = document.RootElement;
            if (!root.TryGetProperty("description", out var descriptionElement) ||
                !root.TryGetProperty("tags", out var tagsElement) ||
                tagsElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                return;

            photo.Description = descriptionElement.GetString() ?? string.Empty;
            photo.Tags = tagsElement.EnumerateArray()
                .Select(tag => tag.GetString())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            _photos.Update(photo);
        }
        catch (System.Text.Json.JsonException)
        {
            // Leave malformed legacy analysis untouched for manual re-indexing.
        }
    }
}
