using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoIt.Data
{
    public static class LayoutTemplateIds
    {
        public const string PaginaSquare = "pagina-square";
        public const string PaginaFull = "pagina-full";
        public const string Landscape4 = "landscape-4";
        public const string Portraitx4L = "portrait-x-4l";
        public const string FourLxPortrait = "4l-x-portrait";
        public const string TravelStyle = "travel-style";
        public const string Portrait3 = "portrait-3";
        public const string Panorama = "panorama";
        public const string Landscape2 = "landscape-2";
        public const string BannerLandscape2 = "banner-landscape-2";

        public const string SmallSquares = "small-squares";
        public const string OneBig = "one-big";
        // … keep any others you still need
    }

    public static class LayoutTemplates
    {

        public static LayoutTemplate? Get(string id) => All.FirstOrDefault(t => t.Id == id);

        public static IEnumerable<PhotoSlot> MigratePhotos(string id, IEnumerable<PhotoSlot> photos)
        {
            var template = Get(id);
            if (template is null)
                throw new ArgumentException($"No layout template found with ID '{id}'.");

            var existingPhotos = photos.ToDictionary(photo => (photo.Col, photo.Row));
            return template.Slots.Select(templateSlot =>
            {
                if (!existingPhotos.TryGetValue((templateSlot.Col, templateSlot.Row), out var existingSlot))
                    return templateSlot;

                var migratedSlot = templateSlot with { PhotoId = existingSlot.PhotoId };
                migratedSlot.PositionX = NormalizePosition(existingSlot.PositionX);
                migratedSlot.PositionY = NormalizePosition(existingSlot.PositionY);
                migratedSlot.Zoom = NormalizeZoom(existingSlot.Zoom);
                return migratedSlot;
            });
        }

        private static double NormalizePosition(double value) =>
            double.IsFinite(value) ? Math.Clamp(value, 0d, 100d) : 50d;

        private static double NormalizeZoom(double value) =>
            double.IsFinite(value) ? Math.Clamp(value, 0.5d, 3d) : 1d;

        public static IReadOnlyList<LayoutTemplate> All { get; } =
        [
            // 1 large square page (Pagina 4)
            new(LayoutTemplateIds.PaginaSquare, "Pagina Square",
            columns: 1, rows: 1,
            [
                new(1, 1, PhotoType.Pagina4)
            ]),

        // 1 large landscape page (Pagina L)
        new(LayoutTemplateIds.PaginaFull, "Pagina Full",
            columns: 1, rows: 1,
            [
                new(1, 1, PhotoType.PaginaL)
            ]),

        // 4 landscape photos (2×2)
        new(LayoutTemplateIds.Landscape4, "Landscape ×4",
            columns: 2, rows: 2,
            [
                new(1, 1, PhotoType.Landscape),
                new(2, 1, PhotoType.Landscape),
                new(1, 2, PhotoType.Landscape),
                new(2, 2, PhotoType.Landscape)
            ]),

        // 1 portrait left + 4 landscapes right
        new(LayoutTemplateIds.Portraitx4L, "Portrait + Landscape ×4",
            columns: 3, rows: 2,
            [
                new(1, 1, PhotoType.Portrait, RowSpan: 2),   // tall portrait left
                // right side 2×2 landscapes
                new(2, 1, PhotoType.Landscape),
                new(3, 1, PhotoType.Landscape),
                new(2, 2, PhotoType.Landscape),
                new(3, 2, PhotoType.Landscape)
            ]),

        // Mirror: 4 landscapes left + 1 portrait right
        new(LayoutTemplateIds.FourLxPortrait, "Landscape ×4 + Portrait",
            columns: 3, rows: 2,
            [
                new(1, 1, PhotoType.Landscape),
                new(2, 1, PhotoType.Landscape),
                new(1, 2, PhotoType.Landscape),
                new(2, 2, PhotoType.Landscape),
                new(3, 1, PhotoType.Portrait, RowSpan: 2)
            ]),

        // Travel style page
        // +---------------------------+
        // |         PANORAMA          |
        // +------------+--------------+
        // |  Square  |   PORTRAIT   | 297/210
        // +------------+--------------+ 10+0+20        
       
        new(LayoutTemplateIds.TravelStyle, "Travel Style",
            columns: 2, rows: 3,
            [
                new(1, 1, PhotoType.Banner, ColSpan: 2),      // full-width banner
                new(1, 2, PhotoType.Square),
                new(2, 2, PhotoType.Square),
            ]),

        // 3 portraits side-by-side
        new(LayoutTemplateIds.Portrait3, "Portrait ×3",
            columns: 3, rows: 1,
            [
                new(1, 1, PhotoType.Portrait),
                new(2, 1, PhotoType.Portrait),
                new(3, 1, PhotoType.Portrait)
            ]),

        // Single panorama
        new(LayoutTemplateIds.Panorama, "Panorama",
            columns: 1, rows: 1,
            [
                new(1, 1, PhotoType.Panorama)
            ]),

        // 2 landscape photos side by side
        new(LayoutTemplateIds.Landscape2, "Landscape ×2",
            columns: 2, rows: 1,
            [
                new(1, 1, PhotoType.Landscape),
                new(2, 1, PhotoType.Landscape)
            ]),

        // Banner above 2 landscape photos
        new(LayoutTemplateIds.BannerLandscape2, "Banner + Landscape ×2",
            columns: 2, rows: 3,
            [
                new(1, 1, PhotoType.Banner, ColSpan: 2),
                new(1, 2, PhotoType.Landscape),
                new(2, 2, PhotoType.Landscape)
            ]),

        // Classic 1 large photo
        new(LayoutTemplateIds.OneBig, "1 large photo",
            columns: 1, rows: 1,
            [
                new(1, 1, PhotoType.Square)   // or Pagina4 / PaginaL depending on preference
            ]),
        // Classic 1 large photo
        new(LayoutTemplateIds.SmallSquares, "6 small photo",
            columns: 3, rows: 2,
            [
                new(1, 1, PhotoType.SmallSquare),
                new(2, 1, PhotoType.SmallSquare),
                new(3, 1, PhotoType.SmallSquare),
                new(1, 2, PhotoType.SmallSquare),
                new(2, 2, PhotoType.SmallSquare),
                new(3, 2, PhotoType.SmallSquare)
                ]),
    ];
    }
}
