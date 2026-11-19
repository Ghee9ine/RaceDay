using RaceDay.Api.Services.Storage;

namespace RaceDay.Api.Tests.Infrastructure;

/// <summary>
/// Replaces IBlobStorageService in RaceDayApiFactory so upload tests never need a real Azure
/// account. Records every upload and deletion in memory so a test can assert on exactly what
/// the controller/service asked it to do, not just that an HTTP call returned 200.
/// </summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public List<string> UploadedPaths { get; } = new();
    public List<string> DeletedUrls { get; } = new();

    public Task<string> UploadImageAsync(Stream content, string contentType, string blobPath, CancellationToken ct)
    {
        UploadedPaths.Add(blobPath);
        return Task.FromResult($"https://fake.blob.local/raceday-images/{blobPath}");
    }

    public Task DeleteIfExistsAsync(string? blobUrl, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(blobUrl))
        {
            DeletedUrls.Add(blobUrl);
        }
        return Task.CompletedTask;
    }
}
