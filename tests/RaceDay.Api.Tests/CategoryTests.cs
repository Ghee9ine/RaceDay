using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

public class CategoryTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public CategoryTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ByEventOwner_Returns201()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);

        var response = await client.PostAsJsonAsync($"/api/events/{RaceDayApiFactory.FutureEventAId}/categories", new
        {
            name = "New Category",
            distanceKm = 15.0m,
            entryFee = 100.0m,
            maxParticipants = 50,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ByNonOwnerOrganiser_Returns403()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserBEmail);

        var response = await client.PostAsJsonAsync($"/api/events/{RaceDayApiFactory.FutureEventAId}/categories", new
        {
            name = "Hijack Category",
            distanceKm = 15.0m,
            entryFee = 0m,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_ByParticipant_Returns403()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantAEmail);

        var response = await client.PostAsJsonAsync($"/api/events/{RaceDayApiFactory.FutureEventAId}/categories", new
        {
            name = "Participant Category",
            distanceKm = 15.0m,
            entryFee = 0m,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateNameInSameEvent_Returns400()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);
        var request = new { name = "Duplicate Category Name", distanceKm = 5.0m, entryFee = 0m };

        var first = await client.PostAsJsonAsync($"/api/events/{RaceDayApiFactory.FutureEventAId}/categories", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync($"/api/events/{RaceDayApiFactory.FutureEventAId}/categories", request);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }
}
