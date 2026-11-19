namespace RaceDay.Api.Services.Storage;

/// <summary>
/// Abstraction over Azure Blob Storage so the rest of the API (and its
/// tests) never talk to the Azure SDK directly. Implemented by
/// <see cref="AzureBlobStorageService"/> when a connection string is
/// configured, and by <see cref="NotConfiguredBlobStorageService"/>
/// otherwise, so a fresh clone or the test suite can start without Azure.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>Uploads <paramref name="content"/> to <paramref name="blobPath"/> and returns the
    /// absolute, publicly-readable URL of the stored blob.</summary>
    Task<string> UploadImageAsync(Stream content, string contentType, string blobPath, CancellationToken ct);

    /// <summary>Deletes the blob at <paramref name="blobUrl"/> if it exists. A null or empty URL
    /// is a no-op, so callers can pass a possibly-unset "previous image" URL without checking first.</summary>
    Task DeleteIfExistsAsync(string? blobUrl, CancellationToken ct);
}

/// <summary>Thrown when an upload is attempted but no Blob Storage connection string is configured.
/// Mapped to 503 Service Unavailable by <see cref="Middleware.GlobalExceptionHandler"/>, distinct
/// from a generic InvalidOperationException so that mapping can't accidentally catch an unrelated
/// bug and hide it behind the same friendly 503.</summary>
public class BlobStorageNotConfiguredException : InvalidOperationException
{
    public BlobStorageNotConfiguredException()
        : base("Blob storage is not configured - set ConnectionStrings:BlobStorage")
    {
    }
}

/// <summary>Registered instead of <see cref="AzureBlobStorageService"/> when
/// ConnectionStrings:BlobStorage is absent. Lets the app, and the test suite, start and run
/// normally; only an actual upload attempt fails, with a clear, mapped error instead of a
/// null-reference deep in some Azure SDK call.</summary>
public class NotConfiguredBlobStorageService : IBlobStorageService
{
    public Task<string> UploadImageAsync(Stream content, string contentType, string blobPath, CancellationToken ct)
        => throw new BlobStorageNotConfiguredException();

    public Task DeleteIfExistsAsync(string? blobUrl, CancellationToken ct)
    {
        // A null/empty URL is a genuine no-op per the interface contract
        // (e.g. a first-ever banner with no previous image to clean up) -
        // only a real, non-empty URL with storage unconfigured is an actual
        // error, and that combination can't arise in practice since
        // UploadImageAsync above would already have thrown before any such
        // URL could have been saved.
        if (string.IsNullOrEmpty(blobUrl))
        {
            return Task.CompletedTask;
        }
        throw new BlobStorageNotConfiguredException();
    }
}
