namespace RaceDay.Api.Services.Storage;

public enum ImageValidationOutcome
{
    Valid,
    Missing,
    TooLarge,
    UnsupportedType,
}

public record ImageValidationResult(ImageValidationOutcome Outcome, string? Extension = null)
{
    public bool IsValid => Outcome == ImageValidationOutcome.Valid;
}

/// <summary>
/// Shared validation for both upload endpoints: size, declared content type, and - because a
/// declared Content-Type header is just a client-supplied string, not proof of anything - the
/// file's actual magic bytes, so a renamed .exe can't slip through as "image/png".
/// </summary>
public static class ImageValidator
{
    public const long MaxSizeBytes = 5_242_880; // 5 MB, matches [RequestSizeLimit] on both endpoints.

    private static readonly Dictionary<string, (byte[] Magic, string Extension)> SignaturesByContentType = new()
    {
        ["image/jpeg"] = (new byte[] { 0xFF, 0xD8, 0xFF }, "jpg"),
        ["image/png"] = (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "png"),
        // WebP: "RIFF"....."WEBP" - the four bytes at offset 8 matter as much
        // as the four at offset 0, so this one gets its own check below
        // rather than a single contiguous-prefix comparison.
        ["image/webp"] = (new byte[] { 0x52, 0x49, 0x46, 0x46 }, "webp"),
    };

    public static async Task<ImageValidationResult> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return new ImageValidationResult(ImageValidationOutcome.Missing);
        }
        if (file.Length > MaxSizeBytes)
        {
            return new ImageValidationResult(ImageValidationOutcome.TooLarge);
        }
        if (!SignaturesByContentType.TryGetValue(file.ContentType, out var signature))
        {
            return new ImageValidationResult(ImageValidationOutcome.UnsupportedType);
        }

        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAsync(header.AsMemory(0, Math.Min(12, (int)file.Length)));
            if (read < signature.Magic.Length)
            {
                return new ImageValidationResult(ImageValidationOutcome.UnsupportedType);
            }
        }

        var matchesDeclaredType = header.AsSpan(0, signature.Magic.Length).SequenceEqual(signature.Magic);
        if (file.ContentType == "image/webp")
        {
            // RIFF....WEBP: bytes 0-3 are "RIFF" (checked above), bytes 8-11
            // must be "WEBP" - the 4 bytes in between are a little-endian
            // chunk size that varies per file and carries no signature value.
            var webpTag = new byte[] { 0x57, 0x45, 0x42, 0x50 };
            matchesDeclaredType = matchesDeclaredType && header.AsSpan(8, 4).SequenceEqual(webpTag);
        }

        return matchesDeclaredType
            ? new ImageValidationResult(ImageValidationOutcome.Valid, signature.Extension)
            : new ImageValidationResult(ImageValidationOutcome.UnsupportedType);
    }
}
