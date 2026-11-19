using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Dtos.Uploads;

/// <summary>Binds the multipart form field "file" so Swashbuckle 6.4 renders a file picker for it.</summary>
public record ImageUploadRequest(IFormFile File);
