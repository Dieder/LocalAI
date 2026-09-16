using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace PhotoIt.Data;

public static class PhotoMetadataReader
{
    public static AlbumPhotoMetadata Extract(byte[] imageBytes)
    {
        var metadata = new AlbumPhotoMetadata();

        if (imageBytes.Length == 0)
            return metadata;

        try
        {
            using var stream = new MemoryStream(imageBytes, writable: false);
            var directories = ImageMetadataReader.ReadMetadata(stream);

            var exif = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var gps = directories.FirstOrDefault(directory => directory.Name.Contains("GPS", StringComparison.OrdinalIgnoreCase));

            if (exif is not null)
            {
                if (TryGetDateTime(exif, ExifDirectoryBase.TagDateTimeOriginal, out var dateTaken)
                    || TryGetDateTime(exif, ExifDirectoryBase.TagDateTimeDigitized, out dateTaken)
                    || TryGetDateTime(exif, ExifDirectoryBase.TagDateTime, out dateTaken))
                {
                    if (dateTaken.Kind == DateTimeKind.Unspecified)
                        dateTaken = DateTime.SpecifyKind(dateTaken, DateTimeKind.Local);

                    metadata.DateTakenUtc = dateTaken.ToUniversalTime();
                }
            }

            metadata.CameraMake = ifd0?.GetString(ExifDirectoryBase.TagMake);
            metadata.CameraModel = ifd0?.GetString(ExifDirectoryBase.TagModel);

            if (gps is not null)
            {
                var latitude = TryReadGpsCoordinate(gps, 2, 1, out var lat) ? (double?)lat : null;
                var longitude = TryReadGpsCoordinate(gps, 4, 3, out var lon) ? (double?)lon : null;

                metadata.Latitude = latitude;
                metadata.Longitude = longitude;
            }
        }
        catch
        {
            // Ignore unreadable metadata; the upload should still succeed.
        }

        return metadata;
    }

    private static bool TryGetDateTime(object directory, int tag, out DateTime value)
    {
        value = default;

        var method = directory.GetType().GetMethod("TryGetDateTime", new[] { typeof(int), typeof(DateTime).MakeByRefType() });
        if (method is not null)
        {
            var parameters = new object?[] { tag, null };
            if ((bool)method.Invoke(directory, parameters)!)
            {
                value = (DateTime)parameters[1]!;
                return true;
            }
        }

        var descriptionMethod = directory.GetType().GetMethod("GetDescription", new[] { typeof(int) });
        var raw = descriptionMethod?.Invoke(directory, new object[] { tag }) as string;
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out value);
    }

    private static bool TryReadGpsCoordinate(object gpsDirectory, int coordinateTag, int referenceTag, out double coordinate)
    {
        coordinate = 0d;

        var descriptionMethod = gpsDirectory.GetType().GetMethod("GetDescription", new[] { typeof(int) });
        var coordinates = descriptionMethod?.Invoke(gpsDirectory, new object[] { coordinateTag }) as string;
        var reference = descriptionMethod?.Invoke(gpsDirectory, new object[] { referenceTag }) as string;

        if (string.IsNullOrWhiteSpace(coordinates))
            return false;

        var valueText = coordinates
            .Replace("\u00b0", "")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(" ", "");

        var parts = valueText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
            return false;

        var degrees = ParseFraction(parts[0]);
        var minutes = ParseFraction(parts[1]);
        var seconds = ParseFraction(parts[2]);
        var decimalValue = degrees + (minutes / 60d) + (seconds / 3600d);

        if (string.Equals(reference, "S", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reference, "W", StringComparison.OrdinalIgnoreCase))
        {
            decimalValue *= -1d;
        }

        coordinate = decimalValue;
        return true;
    }

    private static double ParseFraction(string value)
    {
        var parts = value.Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator)
            && denominator != 0)
        {
            return numerator / denominator;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0d;
    }
}

public sealed class AlbumPhotoMetadata
{
    public DateTime? DateTakenUtc { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
}
