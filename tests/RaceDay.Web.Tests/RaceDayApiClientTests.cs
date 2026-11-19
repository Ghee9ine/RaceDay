using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services.Api;
using RaceDay.Web.Tests.Infrastructure;

namespace RaceDay.Web.Tests;

public class RaceDayApiClientTests
{
    private static RaceDayApiClient BuildClient(CapturingHandler handler, IHttpContextAccessor? accessor = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new RaceDayApiClient(http, accessor ?? new HttpContextAccessor());
    }

    [Fact]
    public async Task Login_Success_ExtractsSessionCookieIntoMvcSession()
    {
        var handler = new CapturingHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent(new LoginResponseDto(1, "Thandi Mokoena", "thandi@example.com", "Participant")),
            },
        };
        handler.ResponseToReturn.Headers.Add("Set-Cookie", ".RaceDay.Session=the-session-value; path=/; httponly");

        var session = new FakeSession();
        var httpContext = new DefaultHttpContext { Session = session };
        var client = BuildClient(handler);

        var result = await client.LoginAsync("thandi@example.com", "password123", httpContext);

        Assert.True(result.Success);
        Assert.Equal("the-session-value", session.GetString(SessionKeys.ApiSessionCookie));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsFailureWithoutThrowing()
    {
        var handler = new CapturingHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = JsonContent(new { message = "Invalid email or password." }),
            },
        };
        var httpContext = new DefaultHttpContext { Session = new FakeSession() };
        var client = BuildClient(handler);

        var result = await client.LoginAsync("thandi@example.com", "wrong", httpContext);

        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task ExpectedFailure_WithPlainMessageBody_MapsToErrorMessage()
    {
        var handler = new CapturingHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent(new { message = "Event not found." }),
            },
        };
        var client = BuildClient(handler);

        var result = await client.GetEventAsync(999);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("Event not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task ExpectedFailure_WithValidationProblemDetailsBody_MapsFieldErrors()
    {
        var handler = new CapturingHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent(new
                {
                    title = "One or more validation errors occurred.",
                    errors = new Dictionary<string, string[]> { ["Email"] = new[] { "The Email field is required." } },
                }),
            },
        };
        var client = BuildClient(handler);

        var result = await client.RegisterAsync(new RegisterRequestDto("Name", "", "pw", "Participant", null));

        Assert.False(result.Success);
        Assert.NotNull(result.ValidationErrors);
        Assert.Contains("The Email field is required.", result.ValidationErrors!["Email"]);
    }

    [Fact]
    public async Task UnexpectedUnauthorized_ThrowsApiUnauthorizedException()
    {
        var handler = new CapturingHandler { ResponseToReturn = new HttpResponseMessage(HttpStatusCode.Unauthorized) };
        var client = BuildClient(handler);

        await Assert.ThrowsAsync<ApiUnauthorizedException>(() => client.GetMyProfileAsync());
    }

    private static HttpContent JsonContent(object value)
    {
        var content = System.Net.Http.Json.JsonContent.Create(value);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }
}
