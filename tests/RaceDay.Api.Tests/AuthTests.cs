using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

public class AuthTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public AuthTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_AsParticipant_Returns201()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "New Participant",
            email = "new.participant@test.raceday.local",
            password = "Password123",
            role = "Participant",
            phoneNumber = "0800000001",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/users/me", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Register_AsOrganiser_Returns201()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "New Organiser",
            email = "new.organiser@test.raceday.local",
            password = "Password123",
            role = "Organiser",
            phoneNumber = (string?)null,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            fullName = "Duplicate",
            email = "duplicate@test.raceday.local",
            password = "Password123",
            role = "Participant",
            phoneNumber = (string?)null,
        };

        var first = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Bad Role",
            email = "bad.role@test.raceday.local",
            password = "Password123",
            role = "Admin",
            phoneNumber = (string?)null,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Weak Password",
            email = "weak.password@test.raceday.local",
            password = "abc",
            role = "Participant",
            phoneNumber = (string?)null,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresPassword_NotAsPlainText()
    {
        var client = _factory.CreateClient();
        const string plainTextPassword = "Password123";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Hash Check",
            email = "hash.check@test.raceday.local",
            password = plainTextPassword,
            role = "Participant",
            phoneNumber = (string?)null,
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = await db.Users.SingleAsync(u => u.Email == "hash.check@test.raceday.local");

        Assert.NotEqual(plainTextPassword, stored.PasswordHash);
        Assert.True(stored.PasswordHash.Length > plainTextPassword.Length);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndSetsSessionCookie()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = RaceDayApiFactory.OrganiserAEmail, password = RaceDayApiFactory.DemoPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith(".RaceDay.Session"));

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.Equal("Organiser", body!.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = RaceDayApiFactory.OrganiserAEmail, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_AfterLogin_Returns200WithCorrectRole()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantAEmail);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal("Participant", body!.Role);
    }

    [Fact]
    public async Task Logout_ThenGetMe_Returns401()
    {
        var client = await TestAuthHelpers.LoginAsAsync(_factory.CreateClient(), RaceDayApiFactory.ParticipantBEmail);

        var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    private record LoginResponseDto(int UserId, string FullName, string Email, string Role);
    private record UserProfileDto(int UserId, string FullName, string Email, string Role);
}
