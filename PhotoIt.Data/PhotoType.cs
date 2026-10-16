using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoIt.Data
{
    public sealed class PhotoType
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double WidthCm { get; set; }
        public double HeightCm { get; set; }
        public double AspectRatio { get; set; }          // width / height
        public string ExampleSize { get; set; } = string.Empty;

        // Required by LiteDB when materializing PhotoType from persisted PhotoSlot data.
        public PhotoType()
        {
        }

        private PhotoType(string id, string name, double widthCm, double heightCm, string exampleSize)
        {
            Id = id;
            Name = name;
            WidthCm = widthCm;
            HeightCm = heightCm;
            AspectRatio = widthCm / heightCm;
            ExampleSize = exampleSize;
        }

        public static readonly PhotoType Portrait = new("portrait", "Portret", 10, 15, "10×15 cm");
        public static readonly PhotoType Landscape = new("landscape", "Landschap", 15, 10, "15×10 cm");
        public static readonly PhotoType Square = new("square", "Vierkant", 10, 10, "20×20 cm");

        public static readonly PhotoType SmallSquare = new("small-square", "Klein Vierkant", 5, 5, "5×5 cm");
        public static readonly PhotoType Panorama = new("panorama", "Panorama", 29.7, 14.9, "29.7×14.9 cm");
        public static readonly PhotoType Banner = new("banner", "Banner", 29.7, 7, "29.7×7 cm");
        public static readonly PhotoType Pagina4 = new("pagina4", "Pagina 4", 210, 210, "21×21 cm");
        public static readonly PhotoType PaginaL = new("paginaL", "Pagina L", 297, 210, "29×21 cm");

        public static IReadOnlyList<PhotoType> All { get; } =
        [
            Portrait, Landscape, Square, SmallSquare, Panorama, Banner, Pagina4, PaginaL
        ];

        public static PhotoType? FromId(string id) =>
            All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        public static string ClassifyPhoto(int? pixelWidth, int? pixelHeight)
        {
            if (pixelWidth is not int width || pixelHeight is not int height || width <= 0 || height <= 0)
                return string.Empty;

            return (width / (double)height) switch
            {
                >= 3.0 => "banner",
                >= 1.8 => "panorama",
                > 1.1 => "landscape",
                < 0.9 => "portrait",
                _ => "square"
            };
        }

        public static bool IsCompatible(string slotTypeId, int? pixelWidth, int? pixelHeight)
        {
            var photoType = ClassifyPhoto(pixelWidth, pixelHeight);
            if (string.IsNullOrWhiteSpace(photoType))
                return true;

            return slotTypeId.ToLowerInvariant() switch
            {
                "portrait" => photoType == "portrait",
                "landscape" => photoType == "landscape",
                "square" or "small-square" or "pagina4" => photoType is "portrait" or "landscape" or "square",
                "paginal" => photoType is "portrait" or "landscape" or "panorama",
                "panorama" => photoType is "landscape" or "panorama",
                "banner" => photoType == "panorama",
                _ => true
            };
        }
    }
}
