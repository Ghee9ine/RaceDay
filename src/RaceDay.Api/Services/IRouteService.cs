using RaceDay.Api.Dtos.Routes;

namespace RaceDay.Api.Services;

public interface IRouteService
{
    Task<RouteResponse?> GetByCategoryAsync(int categoryId);
    Task<ServiceResult<RouteResponse>> UpsertAsync(int categoryId, int organiserId, UpsertRouteRequest request);
}
