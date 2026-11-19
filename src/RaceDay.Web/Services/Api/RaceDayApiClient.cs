using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services.Api;

/// <summary>
/// The only thing in this app that talks to the API. Every method returns
/// ApiResult&lt;T&gt; for expected outcomes (400/403/404/409) rather than
/// throwing, except a 401 on an already-logged-in session, which means the
/// API's own session has expired independently of this app's - that's
/// ApiUnauthorizedException, handled globally by
/// ApiUnauthorizedExceptionFilter. Login itself handles its own 401
/// (wrong password) as a normal, expected ApiResult failure, never as that
/// exception - those are two different things that happen to share a
/// status code.
/// </summary>
public class RaceDayApiClient : IRaceDayApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
    }

    // --- Auth ---------------------------------------------------------

    public async Task<ApiResult<LoginResponseDto>> LoginAsync(string email, string password, HttpContext httpContext)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequestDto(email, password));

        if (!response.IsSuccessStatusCode)
        {
            // A login 401 is an entirely normal, expected outcome (wrong
            // credentials) - this is the one place a 401 must NOT become
            // ApiUnauthorizedException, so it's handled directly here
            // rather than through ReadResultAsync's generic rule below.
            return await ToFailureAsync<LoginResponseDto>(response);
        }

        // The API sets .RaceDay.Session via Set-Cookie on this response;
        // HttpClientHandler.UseCookies = false (Program.cs) means it was
        // never auto-stored anywhere, so it has to be read out and saved
        // into THIS user's own MVC session by hand.
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            foreach (var header in setCookieHeaders)
            {
                if (header.StartsWith(".RaceDay.Session=", StringComparison.OrdinalIgnoreCase))
                {
                    var cookieValue = header.Split(';')[0].Split('=', 2)[1];
                    httpContext.Session.SetString(SessionKeys.ApiSessionCookie, cookieValue);
                }
            }
        }

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        return ApiResult<LoginResponseDto>.Ok(body!);
    }

    public async Task<ApiResult<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        return await ReadResultAsync<RegisterResponseDto>(response);
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _http.PostAsync("api/auth/logout", content: null);
        }
        catch (ApiUnauthorizedException)
        {
            // Already logged out on the API side (e.g. the session expired
            // before the user clicked Logout) - nothing further to do.
        }
    }

    // --- Profile --------------------------------------------------------

    public async Task<ApiResult<UserProfileDto>> GetMyProfileAsync()
    {
        var response = await _http.GetAsync("api/users/me");
        return await ReadResultAsync<UserProfileDto>(response);
    }

    public async Task<ApiResult<UserProfileDto>> UpdateMyProfileAsync(UpdateProfileRequestDto request)
    {
        var response = await _http.PutAsJsonAsync("api/users/me", request);
        return await ReadResultAsync<UserProfileDto>(response);
    }

    public async Task<ApiResult<ProfilePictureUploadedDto>> UploadProfilePictureAsync(IFormFile file)
    {
        using var content = await BuildMultipartAsync(file);
        var response = await _http.PostAsync("api/users/me/profile-picture", content);
        return await ReadResultAsync<ProfilePictureUploadedDto>(response);
    }

    // --- Events -----------------------------------------------------------

    public async Task<ApiResult<List<EventSummaryDto>>> GetPublishedEventsAsync(string? province, string? disciplineType, DateOnly? from, DateOnly? to)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(province)) query.Add($"province={Uri.EscapeDataString(province)}");
        if (!string.IsNullOrWhiteSpace(disciplineType)) query.Add($"disciplineType={Uri.EscapeDataString(disciplineType)}");
        if (from is not null) query.Add($"from={from:yyyy-MM-dd}");
        if (to is not null) query.Add($"to={to:yyyy-MM-dd}");
        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);

        var response = await _http.GetAsync(url);
        return await ReadResultAsync<List<EventSummaryDto>>(response);
    }

    public async Task<ApiResult<EventDetailDto>> GetEventAsync(int eventId)
    {
        var response = await _http.GetAsync($"api/events/{eventId}");
        return await ReadResultAsync<EventDetailDto>(response);
    }

    public async Task<ApiResult<List<MyEventDto>>> GetMyEventsAsync()
    {
        var response = await _http.GetAsync("api/events/mine");
        return await ReadResultAsync<List<MyEventDto>>(response);
    }

    public async Task<ApiResult<EventDetailDto>> CreateEventAsync(CreateEventRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/events", request);
        return await ReadResultAsync<EventDetailDto>(response);
    }

    public async Task<ApiResult<EventDetailDto>> UpdateEventAsync(int eventId, UpdateEventRequestDto request)
    {
        var response = await _http.PutAsJsonAsync($"api/events/{eventId}", request);
        return await ReadResultAsync<EventDetailDto>(response);
    }

    public async Task<ApiResult<bool>> DeleteEventAsync(int eventId)
    {
        var response = await _http.DeleteAsync($"api/events/{eventId}");
        return await ReadNoContentResultAsync(response);
    }

    public async Task<ApiResult<BannerUploadedDto>> UploadBannerAsync(int eventId, IFormFile file)
    {
        using var content = await BuildMultipartAsync(file);
        var response = await _http.PostAsync($"api/events/{eventId}/banner", content);
        return await ReadResultAsync<BannerUploadedDto>(response);
    }

    // --- Categories -------------------------------------------------------

    public async Task<ApiResult<List<CategoryDto>>> GetCategoriesForEventAsync(int eventId)
    {
        var response = await _http.GetAsync($"api/events/{eventId}/categories");
        return await ReadResultAsync<List<CategoryDto>>(response);
    }

    public async Task<ApiResult<CategoryDto>> GetCategoryAsync(int categoryId)
    {
        var response = await _http.GetAsync($"api/categories/{categoryId}");
        return await ReadResultAsync<CategoryDto>(response);
    }

    public async Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequestDto request)
    {
        var response = await _http.PostAsJsonAsync($"api/events/{eventId}/categories", request);
        return await ReadResultAsync<CategoryDto>(response);
    }

    public async Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, CategoryRequestDto request)
    {
        var response = await _http.PutAsJsonAsync($"api/categories/{categoryId}", request);
        return await ReadResultAsync<CategoryDto>(response);
    }

    public async Task<ApiResult<bool>> DeleteCategoryAsync(int categoryId)
    {
        var response = await _http.DeleteAsync($"api/categories/{categoryId}");
        return await ReadNoContentResultAsync(response);
    }

    // --- Routes -------------------------------------------------------------

    public async Task<ApiResult<RouteDto>> GetRouteAsync(int categoryId)
    {
        var response = await _http.GetAsync($"api/categories/{categoryId}/route");
        return await ReadResultAsync<RouteDto>(response);
    }

    public async Task<ApiResult<RouteDto>> UpsertRouteAsync(int categoryId, UpsertRouteRequestDto request)
    {
        var response = await _http.PutAsJsonAsync($"api/categories/{categoryId}/route", request);
        return await ReadResultAsync<RouteDto>(response);
    }

    // --- Weather --------------------------------------------------------

    public async Task<ApiResult<WeatherDto>> GetWeatherAsync(int eventId)
    {
        var response = await _http.GetAsync($"api/events/{eventId}/weather");
        return await ReadResultAsync<WeatherDto>(response);
    }

    // --- Enrolments ---------------------------------------------------------

    public async Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId)
    {
        var response = await _http.PostAsync($"api/categories/{categoryId}/enrolments", content: null);
        return await ReadResultAsync<EnrolmentDto>(response);
    }

    public async Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync()
    {
        var response = await _http.GetAsync("api/enrolments/me");
        return await ReadResultAsync<List<EnrolmentDto>>(response);
    }

    public async Task<ApiResult<List<OrganiserEnrolmentDto>>> GetEnrolmentsForEventAsync(int eventId)
    {
        var response = await _http.GetAsync($"api/events/{eventId}/enrolments");
        return await ReadResultAsync<List<OrganiserEnrolmentDto>>(response);
    }

    public async Task<ApiResult<bool>> CancelEnrolmentAsync(int enrolmentId)
    {
        var response = await _http.DeleteAsync($"api/enrolments/{enrolmentId}");
        return await ReadNoContentResultAsync(response);
    }

    // --- Results --------------------------------------------------------

    public async Task<ApiResult<ResultDto>> CaptureResultAsync(int enrolmentId, CaptureResultRequestDto request)
    {
        var response = await _http.PostAsJsonAsync($"api/enrolments/{enrolmentId}/results", request);
        return await ReadResultAsync<ResultDto>(response);
    }

    public async Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, CaptureResultRequestDto request)
    {
        var response = await _http.PutAsJsonAsync($"api/results/{resultId}", request);
        return await ReadResultAsync<ResultDto>(response);
    }

    public async Task<ApiResult<List<MyResultDto>>> GetMyResultsAsync()
    {
        var response = await _http.GetAsync("api/results/me");
        return await ReadResultAsync<List<MyResultDto>>(response);
    }

    public async Task<ApiResult<List<LeaderboardEntryDto>>> GetLeaderboardAsync(int categoryId)
    {
        var response = await _http.GetAsync($"api/categories/{categoryId}/results");
        return await ReadResultAsync<List<LeaderboardEntryDto>>(response);
    }

    // --- Shared plumbing ------------------------------------------------

    private static async Task<MultipartFormDataContent> BuildMultipartAsync(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "file", file.FileName);
        await Task.CompletedTask;
        return content;
    }

    /// <summary>The one place every non-upload, non-login call routes through: success ->
    /// deserialize and wrap; 401 -> ApiUnauthorizedException (session expired mid-use, the only
    /// place this exception is thrown); anything else expected (400/403/404/409) -> a failure
    /// ApiResult a controller can act on.</summary>
    private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiUnauthorizedException();
        }

        if (!response.IsSuccessStatusCode)
        {
            return await ToFailureAsync<T>(response);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return ApiResult<T>.Ok(default!, response.StatusCode);
        }

        var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return ApiResult<T>.Ok(body!, response.StatusCode);
    }

    /// <summary>For DELETE endpoints (204 No Content on success) where there's no body to
    /// deserialize - "did it work" is all the caller needs.</summary>
    private static async Task<ApiResult<bool>> ReadNoContentResultAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiUnauthorizedException();
        }
        if (!response.IsSuccessStatusCode)
        {
            return await ToFailureAsync<bool>(response);
        }
        return ApiResult<bool>.Ok(true, response.StatusCode);
    }

    /// <summary>Parses the API's two different error-body shapes: ASP.NET Core's automatic
    /// [ApiController] model-validation failures (ValidationProblemDetails, an "errors"
    /// dictionary), and this API's own hand-written service errors (a plain { message } object).
    /// Never surfaces raw JSON/exception text - callers get a friendly ErrorMessage or
    /// ValidationErrors to show, nothing else.</summary>
    private static async Task<ApiResult<T>> ToFailureAsync<T>(HttpResponseMessage response)
    {
        string? message = null;
        IDictionary<string, string[]>? validationErrors = null;

        try
        {
            var raw = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                {
                    validationErrors = errorsElement.EnumerateObject()
                        .ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(v => v.GetString() ?? string.Empty).ToArray());
                    message = doc.RootElement.TryGetProperty("title", out var title) ? title.GetString() : "Validation failed.";
                }
                else if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    message = messageElement.GetString();
                }
                else if (doc.RootElement.TryGetProperty("title", out var titleOnly))
                {
                    message = titleOnly.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Not a JSON body at all (e.g. a bare 401 challenge with no
            // content) - fall through with no message rather than let a
            // parse failure become a 500 of this app's own.
        }

        return ApiResult<T>.Fail(response.StatusCode, message, validationErrors);
    }
}
