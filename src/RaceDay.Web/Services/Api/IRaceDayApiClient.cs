using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services.Api;

public interface IRaceDayApiClient
{
    // Auth
    Task<ApiResult<LoginResponseDto>> LoginAsync(string email, string password, HttpContext httpContext);
    Task<ApiResult<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task LogoutAsync();

    // Profile
    Task<ApiResult<UserProfileDto>> GetMyProfileAsync();
    Task<ApiResult<UserProfileDto>> UpdateMyProfileAsync(UpdateProfileRequestDto request);
    Task<ApiResult<ProfilePictureUploadedDto>> UploadProfilePictureAsync(IFormFile file);

    // Events
    Task<ApiResult<List<EventSummaryDto>>> GetPublishedEventsAsync(string? province, string? disciplineType, DateOnly? from, DateOnly? to);
    Task<ApiResult<EventDetailDto>> GetEventAsync(int eventId);
    Task<ApiResult<List<MyEventDto>>> GetMyEventsAsync();
    Task<ApiResult<EventDetailDto>> CreateEventAsync(CreateEventRequestDto request);
    Task<ApiResult<EventDetailDto>> UpdateEventAsync(int eventId, UpdateEventRequestDto request);
    Task<ApiResult<bool>> DeleteEventAsync(int eventId);
    Task<ApiResult<BannerUploadedDto>> UploadBannerAsync(int eventId, IFormFile file);

    // Categories
    Task<ApiResult<List<CategoryDto>>> GetCategoriesForEventAsync(int eventId);
    Task<ApiResult<CategoryDto>> GetCategoryAsync(int categoryId);
    Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequestDto request);
    Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, CategoryRequestDto request);
    Task<ApiResult<bool>> DeleteCategoryAsync(int categoryId);

    // Routes
    Task<ApiResult<RouteDto>> GetRouteAsync(int categoryId);
    Task<ApiResult<RouteDto>> UpsertRouteAsync(int categoryId, UpsertRouteRequestDto request);

    // Weather
    Task<ApiResult<WeatherDto>> GetWeatherAsync(int eventId);

    // Enrolments
    Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId);
    Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync();
    Task<ApiResult<List<OrganiserEnrolmentDto>>> GetEnrolmentsForEventAsync(int eventId);
    Task<ApiResult<bool>> CancelEnrolmentAsync(int enrolmentId);

    // Results
    Task<ApiResult<ResultDto>> CaptureResultAsync(int enrolmentId, CaptureResultRequestDto request);
    Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, CaptureResultRequestDto request);
    Task<ApiResult<List<MyResultDto>>> GetMyResultsAsync();
    Task<ApiResult<List<LeaderboardEntryDto>>> GetLeaderboardAsync(int categoryId);
}
