using RaceDay.Api.Dtos.Results;

namespace RaceDay.Api.Services;

public interface IResultService
{
    Task<ServiceResult<ResultResponse>> CaptureAsync(int enrolmentId, int organiserId, CaptureResultRequest request);
    Task<ServiceResult<ResultResponse>> UpdateAsync(int resultId, int organiserId, CaptureResultRequest request);
    Task<List<MyResultResponse>> GetMineAsync(int participantId);
    Task<List<LeaderboardEntryResponse>?> GetLeaderboardAsync(int categoryId);
}
