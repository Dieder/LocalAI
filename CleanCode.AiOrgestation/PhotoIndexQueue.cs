using System.Collections.Concurrent;
using System.Threading.Channels;
using AiFoundryLocal;
using CleanCode.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanCode.AiOrchestration;

public sealed record AlbumPhotoIndexRequest(string AlbumId, string PhotoId);

public sealed class PhotoIndexQueue
{
    private readonly Channel<AlbumPhotoIndexRequest> _queue =
        Channel.CreateUnbounded<AlbumPhotoIndexRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
    private readonly ConcurrentDictionary<string, byte> _pending = new(StringComparer.OrdinalIgnoreCase);

    public void Enqueue(AlbumPhotoIndexRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_pending.TryAdd(request.PhotoId, 0))
            _queue.Writer.TryWrite(request);
    }

    public bool IsPending(string photoId) => _pending.ContainsKey(photoId);

    public void Complete(string photoId) => _pending.TryRemove(photoId, out _);

    public IAsyncEnumerable<AlbumPhotoIndexRequest> ReadAllAsync(CancellationToken ct)
    {
        return _queue.Reader.ReadAllAsync(ct);
    }
}

public sealed class PhotoIndexBackgroundService : BackgroundService
{
    private readonly PhotoIndexQueue _queue;
    private readonly PhotoVisionOrchestrator _orchestrator;
    private readonly AlbumCatalog _catalog;
    private readonly AzurePhotoStorage _storage;
    private readonly Phi3VisionModel _visionModel;
    private readonly ILogger<PhotoIndexBackgroundService> _logger;

    public PhotoIndexBackgroundService(
        PhotoIndexQueue queue,
        PhotoVisionOrchestrator orchestrator,
        AlbumCatalog catalog,
        AzurePhotoStorage storage,
        Phi3VisionModel visionModel,
        ILogger<PhotoIndexBackgroundService> logger)
    {
        _queue = queue;
        _orchestrator = orchestrator;
        _catalog = catalog;
        _storage = storage;
        _visionModel = visionModel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _visionModel.LoadAsync(stoppingToken);
            _logger.LogInformation("Phi-3 Vision ONNX model loaded and ready.");

            await foreach (var request in _queue.ReadAllAsync(stoppingToken))
            {
                var photo = _catalog.GetPhoto(request.AlbumId, request.PhotoId);
                if (photo is null)
                {
                    _queue.Complete(request.PhotoId);
                    continue;
                }

                try
                {
                    await _orchestrator.IndexPhotoAsync(photo, _catalog, _storage, stoppingToken);
                    _logger.LogInformation("Indexed tags for photo {PhotoId}.", photo.Id);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to index photo {PhotoId}.", photo.Id);
                }
                finally
                {
                    _queue.Complete(request.PhotoId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "The photo indexing service could not load the vision model.");
        }
    }
}
