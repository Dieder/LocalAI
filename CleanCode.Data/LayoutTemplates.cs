namespace CleanCode.Data;

public sealed record LayoutRectangle(int Order, string Size, int Number);

public sealed record LayoutTemplate(
    string Id,
    string Name,
    IReadOnlyList<LayoutRectangle> Rectangles);

public static class LayoutTemplates
{
    public static IReadOnlyList<LayoutTemplate> All { get; } =
    [
        new(LayoutTemplateIds.OneBig, "1 large photo",
        [
            new(1, "full", 1),
        ]),
        new(LayoutTemplateIds.PanoramaTwoSmall, "2 landscape",
        [
            new(1, "landscape", 1),
            new(2, "landscape", 2),
        ]),
        new(LayoutTemplateIds.FourPortrait, "4 portrait",
        [
            new(1, "portrait", 1),
            new(2, "portrait", 2),
            new(3, "portrait", 3),
            new(4, "portrait", 4),
        ]),
        new(LayoutTemplateIds.PanoramaFourSmall, "1 panorama + 4 small",
        [
            new(1, "panorama", 1),
            new(2, "small", 2),
            new(3, "small", 3),
            new(4, "small", 4),
            new(5, "small", 5),
        ]),
        new(LayoutTemplateIds.PortraitFourSmall, "1 portrait left + 3 small",
        [
            new(1, "portrait", 1),
            new(2, "small", 2),
            new(3, "small", 3),
            new(4, "small", 4),
        ]),
        new(LayoutTemplateIds.NineSmall, "9 small",
        [
            new(1, "small", 1),
            new(2, "small", 2),
            new(3, "small", 3),
            new(4, "small", 4),
            new(5, "small", 5),
            new(6, "small", 6),
            new(7, "small", 7),
            new(8, "small", 8),
            new(9, "small", 9),
        ]),
    ];

    public static LayoutTemplate Get(string templateId)
    {
        return All.FirstOrDefault(t => t.Id == templateId)
            ?? All[0];
    }

    public static List<PagePhotoSlot> CreateEmptySlots(string templateId)
    {
        return Get(templateId).Rectangles
            .OrderBy(r => r.Order)
            .Select(r => new PagePhotoSlot { Number = r.Number })
            .ToList();
    }

    public static List<PagePhotoSlot> MigratePhotos(string newTemplateId, IReadOnlyList<PagePhotoSlot> existingSlots)
    {
        var photoIds = existingSlots
            .OrderBy(s => s.Number)
            .Select(s => s.PhotoId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

        var newSlots = CreateEmptySlots(newTemplateId);
        for (var i = 0; i < newSlots.Count && i < photoIds.Count; i++)
            newSlots[i].PhotoId = photoIds[i];

        return newSlots;
    }
}
