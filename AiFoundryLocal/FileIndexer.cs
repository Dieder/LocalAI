using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;

namespace AiFoundryLocal
{
    public class FileMetadata
    {
        public string Path { get; init; } = string.Empty;
        public string Extension { get; init; } = string.Empty;
        public long Size { get; init; }
        public DateTime CreatedUtc { get; init; }
        public DateTime ModifiedUtc { get; init; }
        public IDictionary<string, string> Exif { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public IList<string> VisionTags { get; init; } = new List<string>();
    }

    public interface IFileVisitor
    {
        Task VisitAsync(FileMetadata file, CancellationToken ct = default);
    }

    public class FileIndexer
    {
        private readonly string _rootDirectory;
        private static readonly HashSet<string> _extensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".raw",
            ".rw2",
            ".png",
            ".jpg",
            ".jpeg"
        };

        public FileIndexer(string rootDirectory)
        {
            _rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
        }

        public async IAsyncEnumerable<FileMetadata> EnumerateAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            if (!Directory.Exists(_rootDirectory)) yield break;

            var dirs = new Stack<string>();
            dirs.Push(_rootDirectory);

            while (dirs.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                var dir = dirs.Pop();
                string[] subDirs = Array.Empty<string>();
                try { subDirs = Directory.GetDirectories(dir); } catch { }
                foreach (var sd in subDirs) dirs.Push(sd);

                string[] files = Array.Empty<string>();
                try { files = Directory.GetFiles(dir); } catch { }
                foreach (var f in files)
                {
                    ct.ThrowIfCancellationRequested();
                    var ext = System.IO.Path.GetExtension(f);
                    if (string.IsNullOrEmpty(ext)) continue;
                    if (!_extensions.Contains(ext)) continue;

                    FileInfo? fi = null;
                    try { fi = new FileInfo(f); } catch { continue; }

                    var meta = new FileMetadata
                    {
                        Path = f,
                        Extension = ext,
                        Size = fi.Length,
                        CreatedUtc = fi.CreationTimeUtc,
                        ModifiedUtc = fi.LastWriteTimeUtc,
                        Exif = ReadExif(f),
                    };

                    yield return meta;

                    // yield control to keep the enumeration asynchronous-friendly
                    await Task.Yield();
                }
            }
        }

        private static IDictionary<string, string> ReadExif(string path)
        {
            var exif = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var image = new MagickImage(path);
                var profile = image.GetExifProfile();
                if (profile is null)
                    return exif;

                foreach (var value in profile.Values)
                {
                    var text = value.GetValue()?.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                        exif[value.Tag.ToString()] = text;
                }
            }
            catch (MagickException)
            {
                // Keep indexing metadata even when a file has no readable EXIF profile.
            }

            return exif;
        }

        public async Task AcceptAsync(IFileVisitor visitor, CancellationToken ct = default)
        {
            await foreach (var file in EnumerateAsync(ct))
            {
                ct.ThrowIfCancellationRequested();
                await visitor.VisitAsync(file, ct).ConfigureAwait(false);
            }
        }
    }
}
