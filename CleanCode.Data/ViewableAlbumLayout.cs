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

public sealed class LayoutPage
{
    public int Number { get; set; }
    public string TemplateId { get; set; } = LayoutTemplateIds.OneBig;
    public List<PagePhotoSlot> Photos { get; set; } = [];
}

public sealed class PagePhotoSlot
{
    public int Number { get; set; }
    public string? PhotoId { get; set; }
}

public static class LayoutTemplateIds
{
    public const string OneBig = "OneBig";
    public const string PanoramaTwoSmall = "PanoramaTwoSmall";
    public const string PanoramaFourSmall = "PanoramaFourSmall";
    public const string FourEqual = "FourEqual";
    public const string FourPortrait = "FourPortrait";
    public const string PortraitFourSmall = "PortraitFourSmall";
    public const string OnePortrait = "OnePortrait";
    public const string NineSmall = "NineSmall";
}
