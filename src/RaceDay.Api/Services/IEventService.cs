using RaceDay.Api.Dtos.Events;

namespace RaceDay.Api.Services;

public record EventFilter(string? Province, string? DisciplineType, DateOnly? From, DateOnly? To);

public interface IEventService
{
    Task<List<EventSummaryResponse>> GetPublishedAsync(EventFilter filter);
    Task<EventDetailResponse?> GetByIdAsync(int eventId, int? callerUserId);
    Task<List<MyEventResponse>> GetMineAsync(int organiserId);
    Task<EventDetailResponse> CreateAsync(int organiserId, CreateEventRequest request);
    Task<ServiceResult<EventDetailResponse>> UpdateAsync(int eventId, int organiserId, UpdateEventRequest request);
    Task<ServiceResult<bool>> DeleteAsync(int eventId, int organiserId);
}
