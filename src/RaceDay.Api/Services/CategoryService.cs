using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Categories;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly RaceDayDbContext _db;

    public CategoryService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryResponse>?> GetByEventAsync(int eventId)
    {
        var eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
        {
            return null;
        }

        // Projected inline, not via the ToResponse helper: a method call
        // inside Select() can't be translated to SQL by EF Core.
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .Select(c => new CategoryResponse(c.CategoryId, c.EventId, c.Name, c.DistanceKm, c.EntryFee, c.MaxParticipants, c.AgeGroup))
            .ToListAsync();
    }

    public async Task<CategoryResponse?> GetByIdAsync(int categoryId)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.CategoryId == categoryId)
            .Select(c => new CategoryResponse(c.CategoryId, c.EventId, c.Name, c.DistanceKm, c.EntryFee, c.MaxParticipants, c.AgeGroup))
            .SingleOrDefaultAsync();
    }

    public async Task<ServiceResult<CategoryResponse>> CreateAsync(int eventId, int organiserId, CategoryRequest request)
    {
        var ev = await _db.Events.SingleOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null)
        {
            return ServiceResult<CategoryResponse>.NotFound();
        }
        if (ev.OrganiserId != organiserId)
        {
            return ServiceResult<CategoryResponse>.Forbidden();
        }

        var duplicateExists = await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == request.Name);
        if (duplicateExists)
        {
            return ServiceResult<CategoryResponse>.ValidationFailed($"A category named '{request.Name}' already exists for this event.");
        }

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name,
            DistanceKm = request.DistanceKm,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants,
            AgeGroup = request.AgeGroup,
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return ServiceResult<CategoryResponse>.Success(ToResponse(category));
    }

    public async Task<ServiceResult<CategoryResponse>> UpdateAsync(int categoryId, int organiserId, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null)
        {
            return ServiceResult<CategoryResponse>.NotFound();
        }
        if (category.Event.OrganiserId != organiserId)
        {
            return ServiceResult<CategoryResponse>.Forbidden();
        }

        var duplicateExists = await _db.Categories.AnyAsync(c => c.EventId == category.EventId && c.Name == request.Name && c.CategoryId != categoryId);
        if (duplicateExists)
        {
            return ServiceResult<CategoryResponse>.ValidationFailed($"A category named '{request.Name}' already exists for this event.");
        }

        category.Name = request.Name;
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;
        category.AgeGroup = request.AgeGroup;
        await _db.SaveChangesAsync();

        return ServiceResult<CategoryResponse>.Success(ToResponse(category));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int categoryId, int organiserId)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .Include(c => c.CourseRoute)
            .SingleOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category is null)
        {
            return ServiceResult<bool>.NotFound();
        }
        if (category.Event.OrganiserId != organiserId)
        {
            return ServiceResult<bool>.Forbidden();
        }
        if (category.Enrolments.Count > 0)
        {
            return ServiceResult<bool>.Conflict();
        }

        if (category.CourseRoute is not null)
        {
            _db.Routes.Remove(category.CourseRoute);
        }
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    private static CategoryResponse ToResponse(Category c) =>
        new(c.CategoryId, c.EventId, c.Name, c.DistanceKm, c.EntryFee, c.MaxParticipants, c.AgeGroup);
}
