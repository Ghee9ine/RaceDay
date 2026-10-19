using System.Net.Http.Json;

namespace RaceDay.Api.Tests.Infrastructure;

public static class TestAuthHelpers
{
    /// <summary>Logs the given client in and returns it, so the session cookie
    /// WebApplicationFactoryClientOptions.HandleCookies (on by default) keeps is attached to every
    /// later request made with the same HttpClient instance.</summary>
    public static async Task<HttpClient> LoginAsAsync(HttpClient client, string email, string password = RaceDayApiFactory.DemoPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return client;
    }
}
