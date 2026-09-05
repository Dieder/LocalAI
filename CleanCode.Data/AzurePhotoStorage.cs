using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CleanCode.Data;

public sealed record AzurePhotoStorageOptions(
    string AccountName,
    string ContainerName = "photos",
    string? AccountUri = null,
    string? AccountKey = null);

public sealed class AzurePhotoStorage
{
    private readonly BlobContainerClient _container;

    public AzurePhotoStorage(AzurePhotoStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.AccountName))
            throw new InvalidOperationException("Azure storage account name is not configured.");

        var accountUri = new Uri(options.AccountUri ?? $"https://{options.AccountName}.blob.core.windows.net");
        BlobServiceClient serviceClient;
        if (!string.IsNullOrWhiteSpace(options.AccountKey))
        {
            serviceClient = new BlobServiceClient(accountUri, new StorageSharedKeyCredential(options.AccountName, options.AccountKey));
        }
        else
        {
            serviceClient = new BlobServiceClient(accountUri, new DefaultAzureCredential());
        }

        _container = serviceClient.GetBlobContainerClient(options.ContainerName);
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, ct);
    }

    public Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        return _container.GetBlobClient(blobName)
            .DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
    }

    public async Task<(Stream Content, string ContentType)> DownloadAsync(
        string blobName,
        CancellationToken ct = default)
    {
        var response = await _container.GetBlobClient(blobName).DownloadStreamingAsync(cancellationToken: ct);
        return (response.Value.Content, response.Value.Details.ContentType ?? "application/octet-stream");
    }

    public Uri GetReadUri(string blobName)
    {
        return _container.GetBlobClient(blobName).Uri;
    }
}