using RaceDay.Api.Dtos.Enrolments;

namespace RaceDay.Api.Services;

public interface IEnrolmentService
{
    Task<ServiceResult<EnrolmentResponse>> EnrolAsync(int participantId, int categoryId);
    Task<List<EnrolmentResponse>> GetMineAsync(int participantId);
    Task<ServiceResult<List<OrganiserEnrolmentResponse>>> GetByEventAsync(int eventId, int organiserId);
    Task<ServiceResult<bool>> CancelAsync(int enrolmentId, int participantId);
}
