using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

/// <summary>
/// Covers both Part 3 upload endpoints against a FakeBlobStorageService (RaceDayApiFactory),
/// so these run with no real Azure account: banner upload (ownership, validation, old-blob
/// cleanup) and profile picture upload (any logged-in user).
/// </summary>
public class UploadTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public UploadTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    // Minimal, genuinely valid magic-byte prefixes - just enough for
    // ImageValidator's signature check to pass; the rest of the bytes are
    // arbitrary padding, since nothing in this codebase actually decodes
    // the image.
    private static readonly byte[] ValidPngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03, 0x04 };
    private static readonly byte[] ValidJpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };

    private static MultipartFormDataContent BuildMultipart(byte[] bytes, string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private async Task<int> CreateEventAsync(HttpClient organiserClient)
    {
        var response = await organiserClient.PostAsJsonAsync("/api/events", new
        {
            name = "Upload Test Event",
            description = (string?)null,
            disciplineType = "Running",
            eventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            startTime = (TimeOnly?)null,
            province = (string?)null,
            venue = (string?)null,
            status = "Published",
        });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<EventCreatedDto>();
        return created!.EventId;
    }

    private record EventCreatedDto(int EventId);
    private record BannerUploadedDto(string BannerImageUrl);
    private record ProfilePictureUploadedDto(string ProfilePictureUrl);

    [Fact]
    public async Task UploadBanner_ByOwner_Returns200AndSavesUrl()
    {
        var client = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(client, RaceDayApiFactory.OrganiserAEmail);
        var eventId = await CreateEventAsync(client);

        var response = await client.PostAsync($"/api/events/{eventId}/banner", BuildMultipart(ValidPngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BannerUploadedDto>();
        Assert.False(string.IsNullOrEmpty(body!.BannerImageUrl));

        var eventResponse = await client.GetAsync($"/api/events/{eventId}");
        var eventJson = await eventResponse.Content.ReadFromJsonAsync<EventDetailDto>();
        Assert.Equal(body.BannerImageUrl, eventJson!.BannerImageUrl);
    }

    private record EventDetailDto(string? BannerImageUrl);

    [Fact]
    public async Task UploadBanner_ByNonOwnerOrganiser_Returns403()
    {
        var ownerClient = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(ownerClient, RaceDayApiFactory.OrganiserAEmail);
        var eventId = await CreateEventAsync(ownerClient);

        var otherClient = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(otherClient, RaceDayApiFactory.OrganiserBEmail);

        var response = await otherClient.PostAsync($"/api/events/{eventId}/banner", BuildMultipart(ValidPngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ByParticipant_Returns403()
    {
        var ownerClient = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(ownerClient, RaceDayApiFactory.OrganiserAEmail);
        var eventId = await CreateEventAsync(ownerClient);

        var participantClient = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(participantClient, RaceDayApiFactory.ParticipantAEmail);

        var response = await participantClient.PostAsync($"/api/events/{eventId}/banner", BuildMultipart(ValidPngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_Anonymous_Returns401()
    {
        var ownerClient = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(ownerClient, RaceDayApiFactory.OrganiserAEmail);
        var eventId = await CreateEventAsync(ownerClient);

        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.PostAsync($"/api/events/{eventId}/banner", BuildMultipart(ValidPngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_UnknownEvent_Returns404()
    {
        var client = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(client, RaceDayApiFactory.OrganiserAEmail);

        var response = await client.PostAsync("/api/events/999999/banner", BuildMultipart(ValidPngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_EmptyFile_Returns400()
    {
        var client = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(client, RaceDayApiFactory.OrganiserAEmail);
        var eventId = await CreateEventAsync(client);

        var response = await client.PostAsync($"/api/events/{eventId}/banner", BuildMultipart(Array.Empty<byte>(), "empty.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WrongContentType_Returns400()
    {
        var client = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(client, RaceDayApiFactory.OrganiserAEmail);
        var eventId = await CreateEventAsync(client);

        var response = await client.PostAsync($"/api/events/{eventId}/banner", BuildMultipart(new byte[] { 1, 2, 3, 4 }, "notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_RenamedExecutableClaimingToBePng_Returns400()
    {
        // The exact scenario the magic-byte check exists for: a file whose
        // Content-Type header claims image/png but whose actual bytes are
        // not a PNG signature at all (an "MZ" DOS header, the real start of
        // a Windows executable, here standing in for one).
        var client = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(client, RaceDayApiFactory.OrganiserAEmail);
        var eventId = await CreateEventAsync(client);

        var fakeExeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        var response = await client.PostAsync($"/api/events/{eventId}/banner", BuildMultipart(fakeExeBytes, "totally-a-banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_OverSizeLimit_Returns400()
    {
        var client = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(client, RaceDayApiFactory.OrganiserAEmail);
        var eventId = await CreateEventAsync(client);

        // One byte over the 5 MB limit, with a valid PNG header so this
        // fails on size specifically, not on the signature check.
        var oversized = new byte[5_242_881];
        ValidPngBytes.CopyTo(oversized, 0);
        var response = await client.PostAsync($"/api/events/{eventId}/banner", BuildMultipart(oversized, "big.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_ByAnyLoggedInUser_Returns200AndShowsInMe()
    {
        var client = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(client, RaceDayApiFactory.ParticipantAEmail);

        var response = await client.PostAsync("/api/users/me/profile-picture", BuildMultipart(ValidJpegBytes, "me.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProfilePictureUploadedDto>();

        var meResponse = await client.GetAsync("/api/users/me");
        var me = await meResponse.Content.ReadFromJsonAsync<ProfileDto>();
        Assert.Equal(body!.ProfilePictureUrl, me!.ProfilePictureUrl);
    }

    private record ProfileDto(string? ProfilePictureUrl);

    [Fact]
    public async Task UploadBanner_ReplacingExistingBanner_DeletesOldBlob()
    {
        var client = _factory.CreateClient();
        await TestAuthHelpers.LoginAsAsync(client, RaceDayApiFactory.OrganiserAEmail);
        var eventId = await CreateEventAsync(client);

        var first = await client.PostAsync($"/api/events/{eventId}/banner", BuildMultipart(ValidPngBytes, "first.png", "image/png"));
        var firstUrl = (await first.Content.ReadFromJsonAsync<BannerUploadedDto>())!.BannerImageUrl;

        await client.PostAsync($"/api/events/{eventId}/banner", BuildMultipart(ValidJpegBytes, "second.jpg", "image/jpeg"));

        Assert.Contains(firstUrl, _factory.BlobStorage.DeletedUrls);
    }
}
