using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services.Storage;

public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";
    public string ContainerName { get; set; } = "raceday-images";
}

/// <summary>
/// Real Azure Blob Storage implementation, registered only when
/// ConnectionStrings:BlobStorage is present (see Program.cs). The container
/// uses blob-level public read (PublicAccessType.Blob) rather than a SAS
/// token per blob: these URLs are stored long-term on Events/Users rows and
/// displayed directly in &lt;img src&gt;, and a SAS token would eventually
/// expire and break every image that was ever uploaded. Public read on a
/// container of resized event banners and profile pictures - never answer
/// keys or anything sensitive - is an acceptable trade for not having to
/// re-sign or refresh URLs forever.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private bool _containerEnsured;
    private readonly SemaphoreSlim _ensureLock = new(1, 1);

    public AzureBlobStorageService(string connectionString, IOptions<BlobStorageOptions> options)
    {
        var serviceClient = new BlobServiceClient(connectionString);
        _containerClient = serviceClient.GetBlobContainerClient(options.Value.ContainerName);
    }

    private async Task EnsureContainerAsync(CancellationToken ct)
    {
        if (_containerEnsured)
        {
            return;
        }
        await _ensureLock.WaitAsync(ct);
        try
        {
            if (!_containerEnsured)
            {
                await _containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
                _containerEnsured = true;
            }
        }
        finally
        {
            _ensureLock.Release();
        }
    }

    public async Task<string> UploadImageAsync(Stream content, string contentType, string blobPath, CancellationToken ct)
    {
        await EnsureContainerAsync(ct);

        var blobClient = _containerClient.GetBlobClient(blobPath);
        var headers = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(content, new BlobUploadOptions { HttpHeaders = headers }, ct);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteIfExistsAsync(string? blobUrl, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(blobUrl))
        {
            return;
        }

        // The stored value is the blob's own absolute URL (e.g.
        // https://<account>.blob.core.windows.net/raceday-images/events/12/<guid>.jpg);
        // the blob's name within the container is everything after the
        // container segment, which Uri.Segments gives us without any
        // string-splitting of the account/container prefix.
        var uri = new Uri(blobUrl);
        var blobName = string.Join(string.Empty, uri.Segments.Skip(2)).TrimStart('/');
        if (blobName.Length == 0)
        {
            return;
        }

        var blobClient = _containerClient.GetBlobClient(Uri.UnescapeDataString(blobName));
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
