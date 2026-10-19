using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

// Every test builds its own fresh category/enrolment via the API, for the
// same reason as EnrolmentTests: shared fixture rows would collide across
// test methods whose execution order isn't guaranteed.
public class ResultTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public ResultTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(int CategoryId, int EnrolmentId)> CreateEnrolledParticipantAsync(string participantEmail, string? categoryName = null, int maxParticipants = 10)
    {
        var organiserClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);
        var category = await (await organiserClient.PostAsJsonAsync($"/api/events/{RaceDayApiFactory.FutureEventAId}/categories", new
        {
            name = categoryName ?? $"Result Test Category {Guid.NewGuid():N}",
            distanceKm = 5.0m,
            entryFee = 0m,
            maxParticipants,
        })).Content.ReadFromJsonAsync<CategoryDto>();

        var participantClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), participantEmail);
        var enrolment = await (await participantClient.PostAsync($"/api/categories/{category!.CategoryId}/enrolments", null)).Content.ReadFromJsonAsync<EnrolmentDto>();

        return (category.CategoryId, enrolment!.EnrolmentId);
    }

    [Fact]
    public async Task Capture_ByEventOwner_Returns201()
    {
        var (_, enrolmentId) = await CreateEnrolledParticipantAsync(RaceDayApiFactory.ParticipantBEmail);
        var organiserClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);

        var response = await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", new { finishTime = "01:00:00", position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Capture_Duplicate_Returns409()
    {
        var (_, enrolmentId) = await CreateEnrolledParticipantAsync(RaceDayApiFactory.ParticipantBEmail);
        var organiserClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);
        var request = new { finishTime = "01:00:00", position = 1, status = "Finished" };

        var first = await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Capture_ByParticipant_Returns403()
    {
        var (_, enrolmentId) = await CreateEnrolledParticipantAsync(RaceDayApiFactory.ParticipantBEmail);
        var participantClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);

        var response = await participantClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", new { finishTime = "01:00:00", position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_ShowsPositionAndTotalFinishers()
    {
        var (_, enrolmentId) = await CreateEnrolledParticipantAsync(RaceDayApiFactory.ParticipantBEmail, categoryName: "GetMine Results Category");
        var organiserClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);
        await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", new { finishTime = "01:23:45", position = 7, status = "Finished" });

        var participantClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);
        var response = await participantClient.GetAsync("/api/results/me");
        var results = await response.Content.ReadFromJsonAsync<List<MyResultDto>>();

        var mine = Assert.Single(results!, r => r.CategoryName == "GetMine Results Category");
        Assert.Equal(7, mine.Position);
        Assert.True(mine.TotalFinishers >= 1);
    }

    [Fact]
    public async Task Leaderboard_OrderedByFinishTime()
    {
        var organiserClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);
        var category = await (await organiserClient.PostAsJsonAsync($"/api/events/{RaceDayApiFactory.FutureEventAId}/categories", new
        {
            name = "Leaderboard Category",
            distanceKm = 5.0m,
            entryFee = 0m,
            maxParticipants = 10,
        })).Content.ReadFromJsonAsync<CategoryDto>();

        var clientA = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantAEmail);
        var enrolmentA = await (await clientA.PostAsync($"/api/categories/{category!.CategoryId}/enrolments", null)).Content.ReadFromJsonAsync<EnrolmentDto>();

        var clientB = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);
        var enrolmentB = await (await clientB.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null)).Content.ReadFromJsonAsync<EnrolmentDto>();

        // Participant A finishes slower than Participant B.
        await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentA!.EnrolmentId}/results", new { finishTime = "02:00:00", position = 2, status = "Finished" });
        await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentB!.EnrolmentId}/results", new { finishTime = "01:00:00", position = 1, status = "Finished" });

        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/categories/{category.CategoryId}/results");
        var leaderboard = await response.Content.ReadFromJsonAsync<List<LeaderboardEntryDto>>();

        Assert.Equal(2, leaderboard!.Count);
        Assert.Equal(new TimeOnly(1, 0, 0), leaderboard[0].FinishTime);
        Assert.Equal(new TimeOnly(2, 0, 0), leaderboard[1].FinishTime);
    }

    private record CategoryDto(int CategoryId);
    private record EnrolmentDto(int EnrolmentId);
    private record MyResultDto(string CategoryName, int? Position, int TotalFinishers);
    private record LeaderboardEntryDto(TimeOnly? FinishTime, int? Position, string Status);
}
