using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Routes;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class RouteService : IRouteService
{
    private readonly RaceDayDbContext _db;

    public RouteService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<RouteResponse?> GetByCategoryAsync(int categoryId)
    {
        return await _db.Routes
            .AsNoTracking()
            .Where(r => r.CategoryId == categoryId)
            .Select(r => new RouteResponse(r.RouteId, r.CategoryId, r.StartPoint, r.EndPoint, r.ElevationGainM, r.MapUrl))
            .SingleOrDefaultAsync();
    }

    public async Task<ServiceResult<RouteResponse>> UpsertAsync(int categoryId, int organiserId, UpsertRouteRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).Include(c => c.CourseRoute).SingleOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null)
        {
            return ServiceResult<RouteResponse>.NotFound();
        }
        if (category.Event.OrganiserId != organiserId)
        {
            return ServiceResult<RouteResponse>.Forbidden();
        }

        var route = category.CourseRoute;
        if (route is null)
        {
            route = new CourseRoute { CategoryId = categoryId };
            _db.Routes.Add(route);
        }

        route.StartPoint = request.StartPoint;
        route.EndPoint = request.EndPoint;
        route.ElevationGainM = request.ElevationGainM;
        route.MapUrl = request.MapUrl;
        await _db.SaveChangesAsync();

        return ServiceResult<RouteResponse>.Success(new RouteResponse(route.RouteId, route.CategoryId, route.StartPoint, route.EndPoint, route.ElevationGainM, route.MapUrl));
    }
}
