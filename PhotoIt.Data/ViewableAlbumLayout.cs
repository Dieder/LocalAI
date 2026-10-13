using LiteDB;

namespace PhotoIt.Data;

public sealed class ViewableAlbumLayout
{
    [BsonId]
    public string AlbumId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? CoverPhotoId { get; set; }
    public List<LayoutPage> Pages { get; set; } = [];
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}



