using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

// Every test that needs an open category to enrol into creates its own via
// the API, rather than sharing RaceDayApiFactory.OpenCategoryId - xUnit
// doesn't guarantee test method execution order within a class, and two
// tests both enrolling the same participant into the same category would
// collide on the (ParticipantId, CategoryId) unique constraint depending
// on what ran first.
public class EnrolmentTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EnrolmentTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> CreateCategoryAsync(int maxParticipants = 10, string? name = null)
    {
        var organiserClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);
        var response = await organiserClient.PostAsJsonAsync($"/api/events/{RaceDayApiFactory.FutureEventAId}/categories", new
        {
            name = name ?? $"Test Category {Guid.NewGuid():N}",
            distanceKm = 5.0m,
            entryFee = 0m,
            maxParticipants,
        });
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        return category!.CategoryId;
    }

    [Fact]
    public async Task Enrol_ByParticipant_Returns201WithBibNumberAndCorrectLinkage()
    {
        var categoryId = await CreateCategoryAsync();
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);

        var response = await client.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.False(string.IsNullOrEmpty(enrolment!.BibNumber));
        Assert.Equal(RaceDayApiFactory.FutureEventAId, enrolment.EventId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var row = await db.Enrolments.SingleAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        Assert.Equal(RaceDayApiFactory.ParticipantBId, row.ParticipantId);
        Assert.Equal(categoryId, row.CategoryId);
    }

    [Fact]
    public async Task Enrol_SecondTimeSameCategory_Returns409()
    {
        var categoryId = await CreateCategoryAsync();
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);

        var first = await client.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_FullCategory_Returns409()
    {
        // RaceDayApiFactory.FullCategoryId already has Participant A confirmed, at MaxParticipants = 1.
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);

        var response = await client.PostAsync($"/api/categories/{RaceDayApiFactory.FullCategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_ByOrganiser_Returns403()
    {
        var categoryId = await CreateCategoryAsync();
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);

        var response = await client.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Anonymous_Returns401()
    {
        var categoryId = await CreateCategoryAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_ReturnsOwnEnrolment()
    {
        var categoryId = await CreateCategoryAsync(name: "GetMine Test Category");
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);
        await client.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        var response = await client.GetAsync("/api/enrolments/me");
        var enrolments = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>();

        Assert.Contains(enrolments!, e => e.CategoryId == categoryId);
    }

    [Fact]
    public async Task OrganiserOwner_SeesEnrolment_InEventEnrolments()
    {
        var categoryId = await CreateCategoryAsync(name: "Organiser Visibility Category");
        var participantClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);
        await participantClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        var organiserClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);
        var response = await organiserClient.GetAsync($"/api/events/{RaceDayApiFactory.FutureEventAId}/enrolments");
        var enrolments = await response.Content.ReadFromJsonAsync<List<OrganiserEnrolmentDto>>();

        Assert.Contains(enrolments!, e => e.CategoryId == categoryId && e.ParticipantEmail == RaceDayApiFactory.ParticipantBEmail);
    }

    [Fact]
    public async Task NonOwnerOrganiser_CannotSeeEventEnrolments_Returns403()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserBEmail);

        var response = await client.GetAsync($"/api/events/{RaceDayApiFactory.FutureEventAId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_OwnEnrolment_Returns204AndStatusCancelled()
    {
        var categoryId = await CreateCategoryAsync(name: "Cancel Own Category");
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);
        var enrolment = await (await client.PostAsync($"/api/categories/{categoryId}/enrolments", null)).Content.ReadFromJsonAsync<EnrolmentDto>();

        var cancelResponse = await client.DeleteAsync($"/api/enrolments/{enrolment!.EnrolmentId}");

        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var row = await db.Enrolments.SingleAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        Assert.Equal("Cancelled", row.Status);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrolment_Returns403()
    {
        var categoryId = await CreateCategoryAsync(name: "Cancel Others Category");
        var ownerClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);
        var enrolment = await (await ownerClient.PostAsync($"/api/categories/{categoryId}/enrolments", null)).Content.ReadFromJsonAsync<EnrolmentDto>();

        var otherClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantAEmail);
        var response = await otherClient.DeleteAsync($"/api/enrolments/{enrolment!.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReEnrol_AfterCancel_ReactivatesSameEnrolmentAndReturns201()
    {
        var categoryId = await CreateCategoryAsync(name: "Reactivation Category");
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);

        var first = await (await client.PostAsync($"/api/categories/{categoryId}/enrolments", null)).Content.ReadFromJsonAsync<EnrolmentDto>();
        await client.DeleteAsync($"/api/enrolments/{first!.EnrolmentId}");

        var reEnrolResponse = await client.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, reEnrolResponse.StatusCode);
        var second = await reEnrolResponse.Content.ReadFromJsonAsync<EnrolmentDto>();

        Assert.Equal(first.EnrolmentId, second!.EnrolmentId);
        Assert.Equal("Confirmed", second.Status);
    }

    private record CategoryDto(int CategoryId);
    private record EnrolmentDto(int EnrolmentId, int EventId, int CategoryId, string? BibNumber, string Status);
    private record OrganiserEnrolmentDto(int EnrolmentId, int CategoryId, string ParticipantEmail);
}
