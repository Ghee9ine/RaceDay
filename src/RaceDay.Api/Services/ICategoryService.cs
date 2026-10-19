using RaceDay.Api.Dtos.Categories;

namespace RaceDay.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryResponse>?> GetByEventAsync(int eventId);
    Task<CategoryResponse?> GetByIdAsync(int categoryId);
    Task<ServiceResult<CategoryResponse>> CreateAsync(int eventId, int organiserId, CategoryRequest request);
    Task<ServiceResult<CategoryResponse>> UpdateAsync(int categoryId, int organiserId, CategoryRequest request);
    Task<ServiceResult<bool>> DeleteAsync(int categoryId, int organiserId);
}
