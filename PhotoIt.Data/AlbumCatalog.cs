using PhotoIt.Data;
using LiteDB;
using System.Security.Cryptography;

namespace PhotoIt.Data;

public class Album
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public string ImageFolderName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;

    public TripGraph TripGraph { get; set; } = new TripGraph(); 

    public string Intro { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;  

    public string ShareSlug { get; set; } = string.Empty;
    public int ShareSeed { get; set; }
}

public sealed class AlbumPhoto
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string UserId { get; set; } = string.Empty;  
    public string AlbumId { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long Length { get; set; }
    public int? PixelWidth { get; set; }
    public int? PixelHeight { get; set; }
    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DateTakenUtc { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public string? LensModel { get; set; }
    public string? FocalLength { get; set; }
    public string? FocalLength35Mm { get; set; }
    public string? SubjectDistance { get; set; }
    public string? Flash { get; set; }
    public string? Dimensions { get; set; }
    public string? ExposureTime { get; set; }
    public string? FNumber { get; set; }
    public string? IsoSpeed { get; set; }
    public string? ExposureProgram { get; set; }
    public string? WhiteBalance { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public DateTime? IndexedUtc { get; set; }
}

public sealed class AlbumCatalog : IDisposable
{
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<Album> _albums;
    private readonly ILiteCollection<AlbumPhoto> _photos;
    private readonly ILiteCollection<ViewableAlbumLayout> _layouts;

    public AlbumCatalog(string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        _database = new LiteDatabase(databasePath);
        _albums = _database.GetCollection<Album>("albums");
        _photos = _database.GetCollection<AlbumPhoto>("albumPhotos");
        _layouts = _database.GetCollection<ViewableAlbumLayout>("viewableAlbumLayouts");
        _albums.EnsureIndex(album => album.ImageFolderName, unique: true);
        _albums.EnsureIndex(album => album.Name);
        _photos.EnsureIndex(photo => photo.AlbumId);
        _layouts.EnsureIndex(layout => layout.AlbumId, unique: true);
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

    public Album? GetShared(string slug, int seed)
    {
        var normalizedSlug = NormalizeShareSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug) || seed <= 0)
            return null;

        return _albums.FindOne(album => album.ShareSlug == normalizedSlug && album.ShareSeed == seed);
    }

    public void EnsureShareLink(Album album)
    {
        ArgumentNullException.ThrowIfNull(album);

        var slug = FormatShareSlug(album.UserId);
        if (string.Equals(album.ShareSlug, slug, StringComparison.Ordinal) && album.ShareSeed > 0)
            return;

        album.ShareSlug = slug;
        do
        {
            album.ShareSeed = RandomNumberGenerator.GetInt32(100_000, 1_000_000);
        }
        while (_albums.Exists(candidate => candidate.Id != album.Id && candidate.ShareSlug == album.ShareSlug && candidate.ShareSeed == album.ShareSeed));

        _albums.Update(album);
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
        _layouts.Delete(id);
        return _albums.Delete(id);
    }

    public IReadOnlyList<AlbumPhoto> ListPhotos(string albumId)
    {
        var photos = _photos.Find(photo => photo.AlbumId == albumId)
            .OrderByDescending(photo => photo.DateTakenUtc.HasValue
                ? photo.DateTakenUtc.Value.ToLocalTime()
                : photo.UploadedUtc.ToLocalTime())
            .ToList();

        foreach (var photo in photos)
            NormalizeLegacyAnalysis(photo);

        return photos;
    }

    public static DateTime GetDisplayDate(AlbumPhoto photo)
    {
        return photo.DateTakenUtc.HasValue
            ? photo.DateTakenUtc.Value.ToLocalTime()
            : photo.UploadedUtc.ToLocalTime();
    }

    public static DateTime GetDisplayDate(DateTime? dateTakenUtc, DateTime uploadedUtc)
    {
        return dateTakenUtc.HasValue
            ? dateTakenUtc.Value.ToLocalTime()
            : uploadedUtc.ToLocalTime();
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

    public ViewableAlbumLayout? GetLayout(string albumId)
    {
        var layout = _layouts.FindById(albumId);
        if (layout is not null)
            NormalizeLayout(layout);

        return layout;
    }

    public bool HasLayout(string albumId)
    {
        return _layouts.Exists(layout => layout.AlbumId == albumId);
    }

    public void SaveLayout(ViewableAlbumLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (string.IsNullOrWhiteSpace(layout.AlbumId))
            throw new ArgumentException("AlbumId is required.", nameof(layout));

        NormalizeLayout(layout);
        layout.UpdatedUtc = DateTime.UtcNow;
        _layouts.Upsert(layout);
        Touch(layout.AlbumId);
    }

    public bool DeleteLayout(string albumId)
    {
        return _layouts.Delete(albumId);
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    private static string NormalizeFolderName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    public static string FormatShareSlug(string value)
    {
        var source = value.Trim();
        var atIndex = source.IndexOf('@');
        if (atIndex > 0)
            source = source[..atIndex];

        var builder = new System.Text.StringBuilder(source.Length);
        var separatorPending = false;
        foreach (var character in source.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (separatorPending && builder.Length > 0)
                    builder.Append('-');

                builder.Append(character);
                separatorPending = false;
            }
            else if (builder.Length > 0)
            {
                separatorPending = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string NormalizeShareSlug(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static void NormalizeLayout(ViewableAlbumLayout layout)
    {
        layout.Pages ??= [];

        foreach (var page in layout.Pages)
        {
            page.Photos ??= [];

            foreach (var slot in page.Photos)
            {
                if (!double.IsFinite(slot.PositionX))
                    slot.PositionX = 50d;
                else
                    slot.PositionX = Math.Clamp(slot.PositionX, 0d, 100d);

                if (!double.IsFinite(slot.PositionY))
                    slot.PositionY = 50d;
                else
                    slot.PositionY = Math.Clamp(slot.PositionY, 0d, 100d);

                if (!double.IsFinite(slot.Zoom))
                    slot.Zoom = 1d;
                else
                    slot.Zoom = Math.Clamp(slot.Zoom, 1d, 3d);
            }
        }
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
