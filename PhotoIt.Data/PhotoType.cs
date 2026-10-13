using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoIt.Data
{
    public sealed class PhotoType
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double AspectRatio { get; set; }          // width / height
        public string ExampleSize { get; set; } = string.Empty;

        // Required by LiteDB when materializing PhotoType from persisted PhotoSlot data.
        public PhotoType()
        {
        }

        private PhotoType(string id, string name, double aspectRatio, string exampleSize)
        {
            Id = id;
            Name = name;
            AspectRatio = aspectRatio;
            ExampleSize = exampleSize;
        }

        // Pre-defined types (keep the sizes you specified)
        public static readonly PhotoType Portrait = new("portrait", "Portret", 2.0 / 3.0, "20×30 cm");
        public static readonly PhotoType Landscape = new("landscape", "Landschap", 3.0 / 2.0, "30×20 cm");
        public static readonly PhotoType Square = new("square", "Vierkant", 1.0, "20×20 cm");

        public static readonly PhotoType SmallSquare = new("small-square", "Klein Vierkant", 1.0, "90x90 mm");
        public static readonly PhotoType Panorama = new("panorama", "Panorama", 2.0, "30×15 cm");
        public static readonly PhotoType Pagina4 = new("pagina4", "Pagina 4", 1.0, "210×210 cm");
        public static readonly PhotoType PaginaL = new("paginaL", "Pagina L", 3.0 / 2.0, "297×210 cm");

        public static IReadOnlyList<PhotoType> All { get; } =
        [
            Portrait, Landscape, Square, Panorama, Pagina4, PaginaL
        ];

        public static PhotoType? FromId(string id) =>
            All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        public static bool IsCompatible(string slotTypeId, int? pixelWidth, int? pixelHeight)
        {
            if (pixelWidth is not int width || pixelHeight is not int height || width <= 0 || height <= 0)
                return true;

            var photoType = (width / (double)height) switch
            {
                >= 1.8 => "panorama",
                > 1.1 => "landscape",
                < 0.9 => "portrait",
                _ => "square"
            };

            return slotTypeId.ToLowerInvariant() switch
            {
                "portrait" => photoType == "portrait",
                "landscape" or "paginal" => photoType == "landscape",
                "panorama" => photoType == "panorama",
                "square" or "small-square" or "pagina4" => photoType == "square",
                _ => true
            };
        }
    }
}
