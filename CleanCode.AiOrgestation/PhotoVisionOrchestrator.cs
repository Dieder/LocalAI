using AiFoundryLocal;
using PhotoIt.Data;

namespace CleanCode.AiOrchestration;

public sealed record PhotoIndexResult(int Indexed, int Skipped, int Failed);

public sealed record AlbumPhotoIndexResult(int Indexed, int Skipped, int Failed);

public sealed class PhotoVisionOrchestrator
{
    private readonly FileIndexer _fileIndexer;
    private readonly PhotoTagCatalog _catalog;
    private readonly Phi3VisionModel _visionModel;

    public PhotoVisionOrchestrator(
        FileIndexer fileIndexer,
        PhotoTagCatalog catalog,
        Phi3VisionModel visionModel)
    {
        _fileIndexer = fileIndexer;
        _catalog = catalog;
        _visionModel = visionModel;
    }

    public async Task<PhotoIndexResult> IndexAndTagAsync(CancellationToken ct = default)
    {
        var indexed = 0;
        var skipped = 0;
        var failed = 0;

        await foreach (var file in _fileIndexer.EnumerateAsync(ct))
        {
            ct.ThrowIfCancellationRequested();
            var existing = _catalog.FindByPath(file.Path);
            if (existing is not null &&
                existing.ModifiedUtc == file.ModifiedUtc &&
                existing.Size == file.Size &&
                existing.Tags.Count > 0)
            {
                skipped++;
                continue;
            }

            try
            {
                var analysis = await _visionModel.AnalyzeImageAsync(
                    file.Path,
                    "Describe this image. Return only JSON with a concise description and an array of useful lowercase tags.",
                    ct);

                _catalog.Upsert(new PhotoTagRecord
                {
                    Path = file.Path,
                    Description = analysis.Description,
                    Tags = analysis.Tags.ToList(),
                    Size = file.Size,
                    ModifiedUtc = file.ModifiedUtc,
                    IndexedUtc = DateTime.UtcNow,
                });
                indexed++;
            }
            catch when (!ct.IsCancellationRequested)
            {
                failed++;
            }
        }

        return new PhotoIndexResult(indexed, skipped, failed);
    }

    public async Task<AlbumPhotoIndexResult> IndexAlbumPhotosAsync(
        string albumId,
        AlbumCatalog catalog,
        AzurePhotoStorage storage,
        CancellationToken ct = default)
    {
        var indexed = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var photo in catalog.ListPhotos(albumId))
        {
            ct.ThrowIfCancellationRequested();
            if (photo.IndexedUtc is not null && photo.Tags.Count > 0)
            {
                skipped++;
                continue;
            }

            try
            {
                await IndexPhotoAsync(photo, catalog, storage, ct);
                indexed++;
            }
            catch when (!ct.IsCancellationRequested)
            {
                failed++;
            }
        }

        if (indexed > 0)
            catalog.Touch(albumId);

        return new AlbumPhotoIndexResult(indexed, skipped, failed);
    }

    public async Task IndexPhotoAsync(
        AlbumPhoto photo,
        AlbumCatalog catalog,
        AzurePhotoStorage storage,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(photo);
        await using var content = (await storage.DownloadAsync(photo.BlobName, ct)).Content;
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, ct);
        var analysis = await _visionModel.AnalyzeImageAsync(
            memory.ToArray(),
            "Describe this image. Return only JSON with a concise description and an array of useful lowercase tags.",
            ct);

        photo.Description = analysis.Description;
        photo.Tags = analysis.Tags.ToList();
        photo.IndexedUtc = DateTime.UtcNow;
        catalog.UpdatePhoto(photo);
        catalog.Touch(photo.AlbumId);
    }
}
