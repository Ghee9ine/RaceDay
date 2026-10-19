using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Enrolments;
using RaceDay.Api.Models;
using RaceDay.Api.Models.Enums;

namespace RaceDay.Api.Services;

public class EnrolmentService : IEnrolmentService
{
    private readonly RaceDayDbContext _db;

    public EnrolmentService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<EnrolmentResponse>> EnrolAsync(int participantId, int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null)
        {
            return ServiceResult<EnrolmentResponse>.NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (category.Event.Status != EventStatuses.Published || category.Event.EventDate < today)
        {
            return ServiceResult<EnrolmentResponse>.Conflict();
        }

        var existing = await _db.Enrolments.SingleOrDefaultAsync(e => e.ParticipantId == participantId && e.CategoryId == categoryId);
        if (existing is not null && existing.Status == EnrolmentStatuses.Confirmed)
        {
            return ServiceResult<EnrolmentResponse>.Conflict();
        }

        if (category.MaxParticipants is not null)
        {
            var confirmedCount = await _db.Enrolments.CountAsync(e => e.CategoryId == categoryId && e.Status == EnrolmentStatuses.Confirmed);
            if (confirmedCount >= category.MaxParticipants)
            {
                return ServiceResult<EnrolmentResponse>.Conflict();
            }
        }

        Enrolment enrolment;
        if (existing is not null)
        {
            // A previously cancelled enrolment for this exact participant
            // and category: reactivate the same row instead of inserting a
            // new one, since (ParticipantId, CategoryId) is unique.
            existing.Status = EnrolmentStatuses.Confirmed;
            enrolment = existing;
            await _db.SaveChangesAsync();
        }
        else
        {
            enrolment = new Enrolment { ParticipantId = participantId, CategoryId = categoryId, Status = EnrolmentStatuses.Confirmed };
            _db.Enrolments.Add(enrolment);
            await _db.SaveChangesAsync();

            // Needs the generated EnrolmentId first, so this is a second
            // save - format fits nvarchar(10) and is unique by construction.
            enrolment.BibNumber = $"C{categoryId:D2}-{enrolment.EnrolmentId:D5}";
            await _db.SaveChangesAsync();
        }

        return ServiceResult<EnrolmentResponse>.Success(new EnrolmentResponse(
            enrolment.EnrolmentId, category.EventId, category.Event.Name, category.CategoryId, category.Name,
            enrolment.BibNumber, enrolment.EnrolmentDate, enrolment.Status));
    }

    public async Task<List<EnrolmentResponse>> GetMineAsync(int participantId)
    {
        return await _db.Enrolments
            .AsNoTracking()
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new EnrolmentResponse(
                e.EnrolmentId, e.Category.EventId, e.Category.Event.Name, e.CategoryId, e.Category.Name,
                e.BibNumber, e.EnrolmentDate, e.Status))
            .ToListAsync();
    }

    public async Task<ServiceResult<List<OrganiserEnrolmentResponse>>> GetByEventAsync(int eventId, int organiserId)
    {
        var ev = await _db.Events.SingleOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null)
        {
            return ServiceResult<List<OrganiserEnrolmentResponse>>.NotFound();
        }
        if (ev.OrganiserId != organiserId)
        {
            return ServiceResult<List<OrganiserEnrolmentResponse>>.Forbidden();
        }

        var enrolments = await _db.Enrolments
            .AsNoTracking()
            .Where(e => e.Category.EventId == eventId)
            .Select(e => new OrganiserEnrolmentResponse(
                e.EnrolmentId, eventId, ev.Name, e.CategoryId, e.Category.Name,
                e.BibNumber, e.EnrolmentDate, e.Status, e.Participant.FullName, e.Participant.Email))
            .ToListAsync();

        return ServiceResult<List<OrganiserEnrolmentResponse>>.Success(enrolments);
    }

    public async Task<ServiceResult<bool>> CancelAsync(int enrolmentId, int participantId)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Result).SingleOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
        if (enrolment is null)
        {
            return ServiceResult<bool>.NotFound();
        }
        if (enrolment.ParticipantId != participantId)
        {
            return ServiceResult<bool>.Forbidden();
        }
        if (enrolment.Result is not null)
        {
            return ServiceResult<bool>.Conflict();
        }

        enrolment.Status = EnrolmentStatuses.Cancelled;
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }
}
