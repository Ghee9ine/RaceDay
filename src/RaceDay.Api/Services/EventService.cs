using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Models.Enums;

namespace RaceDay.Api.Services;

public class EventService : IEventService
{
    private readonly RaceDayDbContext _db;

    public EventService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<List<EventSummaryResponse>> GetPublishedAsync(EventFilter filter)
    {
        var query = _db.Events.AsNoTracking().Where(e => e.Status == EventStatuses.Published);

        if (!string.IsNullOrWhiteSpace(filter.Province))
        {
            query = query.Where(e => e.Province == filter.Province);
        }
        if (!string.IsNullOrWhiteSpace(filter.DisciplineType))
        {
            query = query.Where(e => e.DisciplineType == filter.DisciplineType);
        }
        if (filter.From is not null)
        {
            query = query.Where(e => e.EventDate >= filter.From);
        }
        if (filter.To is not null)
        {
            query = query.Where(e => e.EventDate <= filter.To);
        }

        // Projected inline (not via the ToSummary helper) so EF Core can
        // translate the whole thing to one SQL query - a helper method call
        // inside Select() can't be translated and throws at runtime.
        return await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventSummaryResponse(
                e.EventId, e.Name, e.Description, e.DisciplineType, e.EventDate, e.StartTime,
                e.Province, e.Venue, e.Status, e.BannerImageUrl, e.Organiser.FullName,
                e.Categories.Select(c => c.DistanceKm).Distinct().OrderBy(d => d).ToArray()))
            .ToListAsync();
    }

    public async Task<EventDetailResponse?> GetByIdAsync(int eventId, int? callerUserId)
    {
        var e = await _db.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .Include(e => e.Organiser)
            .SingleOrDefaultAsync(e => e.EventId == eventId);

        if (e is null)
        {
            return null;
        }

        // A Draft event is invisible to anyone but its own organiser - from
        // the outside it should look exactly like it doesn't exist (404),
        // not reveal its existence via a 403.
        if (e.Status == EventStatuses.Draft && e.OrganiserId != callerUserId)
        {
            return null;
        }

        return ToDetail(e);
    }

    public async Task<List<MyEventResponse>> GetMineAsync(int organiserId)
    {
        return await _db.Events
            .AsNoTracking()
            .Where(e => e.OrganiserId == organiserId)
            .OrderByDescending(e => e.EventDate)
            .Select(e => new MyEventResponse(
                e.EventId,
                e.Name,
                e.DisciplineType,
                e.EventDate,
                e.Status,
                e.Categories.SelectMany(c => c.Enrolments).Count(en => en.Status == EnrolmentStatuses.Confirmed)))
            .ToListAsync();
    }

    public async Task<EventDetailResponse> CreateAsync(int organiserId, CreateEventRequest request)
    {
        var entity = new Event
        {
            OrganiserId = organiserId,
            Name = request.Name,
            Description = request.Description,
            DisciplineType = request.DisciplineType,
            EventDate = request.EventDate,
            StartTime = request.StartTime,
            Province = request.Province,
            Venue = request.Venue,
            Status = request.Status ?? EventStatuses.Published,
            // Set explicitly, not left to the column's SQL Server-only
            // SYSUTCDATETIME() default - the test suite runs on SQLite.
            CreatedAt = DateTime.UtcNow,
        };

        _db.Events.Add(entity);
        await _db.SaveChangesAsync();

        await _db.Entry(entity).Reference(e => e.Organiser).LoadAsync();
        return ToDetail(entity);
    }

    public async Task<ServiceResult<EventDetailResponse>> UpdateAsync(int eventId, int organiserId, UpdateEventRequest request)
    {
        var entity = await _db.Events.Include(e => e.Organiser).Include(e => e.Categories).SingleOrDefaultAsync(e => e.EventId == eventId);
        if (entity is null)
        {
            return ServiceResult<EventDetailResponse>.NotFound();
        }
        if (entity.OrganiserId != organiserId)
        {
            return ServiceResult<EventDetailResponse>.Forbidden();
        }

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.DisciplineType = request.DisciplineType;
        entity.EventDate = request.EventDate;
        entity.StartTime = request.StartTime;
        entity.Province = request.Province;
        entity.Venue = request.Venue;
        entity.Status = request.Status;

        await _db.SaveChangesAsync();

        return ServiceResult<EventDetailResponse>.Success(ToDetail(entity));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int eventId, int organiserId)
    {
        var entity = await _db.Events
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .Include(e => e.Categories).ThenInclude(c => c.CourseRoute)
            .Include(e => e.WeatherSnapshots)
            .SingleOrDefaultAsync(e => e.EventId == eventId);

        if (entity is null)
        {
            return ServiceResult<bool>.NotFound();
        }
        if (entity.OrganiserId != organiserId)
        {
            return ServiceResult<bool>.Forbidden();
        }
        if (entity.Categories.Any(c => c.Enrolments.Count > 0))
        {
            return ServiceResult<bool>.Conflict();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var routes = entity.Categories.Where(c => c.CourseRoute is not null).Select(c => c.CourseRoute!);
        _db.Routes.RemoveRange(routes);
        _db.WeatherSnapshots.RemoveRange(entity.WeatherSnapshots);
        _db.Categories.RemoveRange(entity.Categories);
        _db.Events.Remove(entity);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResult<bool>.Success(true);
    }

    private static EventDetailResponse ToDetail(Event e) => new(
        e.EventId, e.Name, e.Description, e.DisciplineType, e.EventDate, e.StartTime,
        e.Province, e.Venue, e.Status, e.BannerImageUrl, e.Organiser.FullName,
        e.Categories.Select(c => c.DistanceKm).Distinct().OrderBy(d => d).ToArray(),
        e.Categories.Select(c => new CategorySummaryResponse(c.CategoryId, c.Name, c.DistanceKm, c.EntryFee, c.MaxParticipants, c.AgeGroup)).ToArray());
}
