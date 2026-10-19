using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Results;
using RaceDay.Api.Models;
using RaceDay.Api.Models.Enums;

namespace RaceDay.Api.Services;

public class ResultService : IResultService
{
    private readonly RaceDayDbContext _db;

    public ResultService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<ResultResponse>> CaptureAsync(int enrolmentId, int organiserId, CaptureResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .SingleOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment is null)
        {
            return ServiceResult<ResultResponse>.NotFound();
        }
        if (enrolment.Category.Event.OrganiserId != organiserId)
        {
            return ServiceResult<ResultResponse>.Forbidden();
        }
        if (enrolment.Result is not null || enrolment.Status == EnrolmentStatuses.Cancelled)
        {
            return ServiceResult<ResultResponse>.Conflict();
        }

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            CapturedByUserId = organiserId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = request.Status,
            // Set explicitly, not left to the column's SQL Server-only
            // SYSUTCDATETIME() default - the test suite runs on SQLite.
            CapturedAt = DateTime.UtcNow,
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return ServiceResult<ResultResponse>.Success(ToResponse(result));
    }

    public async Task<ServiceResult<ResultResponse>> UpdateAsync(int resultId, int organiserId, CaptureResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .SingleOrDefaultAsync(r => r.ResultId == resultId);

        if (result is null)
        {
            return ServiceResult<ResultResponse>.NotFound();
        }
        if (result.Enrolment.Category.Event.OrganiserId != organiserId)
        {
            return ServiceResult<ResultResponse>.Forbidden();
        }

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = request.Status;
        await _db.SaveChangesAsync();

        return ServiceResult<ResultResponse>.Success(ToResponse(result));
    }

    public async Task<List<MyResultResponse>> GetMineAsync(int participantId)
    {
        var results = await _db.Results
            .AsNoTracking()
            .Where(r => r.Enrolment.ParticipantId == participantId)
            .Select(r => new
            {
                r.Enrolment.Category.Event.Name,
                r.Enrolment.Category.Event.EventDate,
                CategoryName = r.Enrolment.Category.Name,
                r.Enrolment.Category.DistanceKm,
                r.FinishTime,
                r.Position,
                r.Status,
                CategoryId = r.Enrolment.CategoryId,
            })
            .ToListAsync();

        // totalFinishers needs a per-category count, cheaper to fetch once
        // for the distinct categories involved than per-row.
        var categoryIds = results.Select(r => r.CategoryId).Distinct().ToList();
        var finisherCounts = await _db.Results
            .Where(r => categoryIds.Contains(r.Enrolment.CategoryId) && r.Status == ResultStatuses.Finished)
            .GroupBy(r => r.Enrolment.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CategoryId, g => g.Count);

        return results
            .Select(r => new MyResultResponse(
                r.Name, r.EventDate, r.CategoryName, r.DistanceKm, r.FinishTime, r.Position,
                finisherCounts.GetValueOrDefault(r.CategoryId), r.Status))
            .ToList();
    }

    public async Task<List<LeaderboardEntryResponse>?> GetLeaderboardAsync(int categoryId)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.CategoryId == categoryId);
        if (!categoryExists)
        {
            return null;
        }

        var results = await _db.Results
            .AsNoTracking()
            .Where(r => r.Enrolment.CategoryId == categoryId)
            .Select(r => new LeaderboardEntryResponse(r.Enrolment.BibNumber, r.Enrolment.Participant.FullName, r.FinishTime, r.Position, r.Status))
            .ToListAsync();

        return results
            .OrderBy(r => r.Status == ResultStatuses.Finished ? 0 : 1)
            .ThenBy(r => r.FinishTime)
            .ThenBy(r => r.ParticipantName)
            .ToList();
    }

    private static ResultResponse ToResponse(Result r) =>
        new(r.ResultId, r.EnrolmentId, r.FinishTime, r.Position, r.Status, r.CapturedAt);
}
