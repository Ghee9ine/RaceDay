using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

public class EventTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EventTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ByOrganiser_Returns201AndAppearsInPublishedList()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);

        var createResponse = await client.PostAsJsonAsync("/api/events", new
        {
            name = "Newly Created Event",
            description = "Created by a test",
            disciplineType = "Running",
            eventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
            province = "Gauteng",
            venue = "Test Venue",
            status = "Published",
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/events");
        var list = await listResponse.Content.ReadFromJsonAsync<List<EventSummaryDto>>();
        Assert.Contains(list!, e => e.Name == "Newly Created Event");
    }

    [Fact]
    public async Task Update_ByOwner_Returns200()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);

        var created = await (await client.PostAsJsonAsync("/api/events", new
        {
            name = "Owned Event To Update",
            disciplineType = "Running",
            eventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(61)),
            status = "Published",
        })).Content.ReadFromJsonAsync<EventDetailDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/events/{created!.EventId}", new
        {
            name = "Updated Name",
            disciplineType = "Running",
            eventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(61)),
            status = "Published",
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<EventDetailDto>();
        Assert.Equal("Updated Name", updated!.Name);
    }

    [Fact]
    public async Task Update_ByNonOwnerOrganiser_Returns403()
    {
        var ownerClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);
        var created = await (await ownerClient.PostAsJsonAsync("/api/events", new
        {
            name = "Event Owned By A",
            disciplineType = "Running",
            eventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(62)),
            status = "Published",
        })).Content.ReadFromJsonAsync<EventDetailDto>();

        var otherClient = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserBEmail);
        var updateResponse = await otherClient.PutAsJsonAsync($"/api/events/{created!.EventId}", new
        {
            name = "Hijacked",
            disciplineType = "Running",
            eventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(62)),
            status = "Published",
        });

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_OwnEventWithNoEnrolments_Returns204()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserAEmail);
        var created = await (await client.PostAsJsonAsync("/api/events", new
        {
            name = "Event To Delete",
            disciplineType = "Running",
            eventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(63)),
            status = "Published",
        })).Content.ReadFromJsonAsync<EventDetailDto>();

        var deleteResponse = await client.DeleteAsync($"/api/events/{created!.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_EventWithEnrolments_Returns409()
    {
        // FutureEventB has Full Category, which the fixture already enrolled Participant A into.
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserBEmail);

        var response = await client.DeleteAsync($"/api/events/{RaceDayApiFactory.FutureEventBId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_ByParticipant_Returns403()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantAEmail);

        var response = await client.PostAsJsonAsync("/api/events", new { name = "x", disciplineType = "Running", eventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_Anonymous_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", new { name = "x", disciplineType = "Running", eventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublished_FiltersByDisciplineType()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events?disciplineType=Cycling");
        var events = await response.Content.ReadFromJsonAsync<List<EventSummaryDto>>();

        Assert.All(events!, e => Assert.Equal("Cycling", e.DisciplineType));
        Assert.Contains(events!, e => e.EventId == RaceDayApiFactory.FutureEventBId);
    }

    [Fact]
    public async Task GetMine_ReturnsEnrolmentCounts()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.OrganiserBEmail);

        var response = await client.GetAsync("/api/events/mine");
        var events = await response.Content.ReadFromJsonAsync<List<MyEventDto>>();

        var eventB = events!.Single(e => e.EventId == RaceDayApiFactory.FutureEventBId);
        Assert.Equal(1, eventB.TotalEnrolments);
    }

    private record EventSummaryDto(int EventId, string Name, string DisciplineType, string Status);
    private record EventDetailDto(int EventId, string Name, string DisciplineType, string Status);
    private record MyEventDto(int EventId, string Name, string Status, int TotalEnrolments);
}
