using dot_net_core_rest_api.Dtos;

namespace dot_net_core_rest_api.Services;

public interface ISubCategoryService
{
    Task<List<SubCategoryDto>> GetAllAsync(CancellationToken ct);
    Task<List<SubCategoryDto>> GetByCategoryIdAsync(int categoryId, CancellationToken ct);
    Task<SubCategoryDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<SubCategoryDto> CreateAsync(CreateSubCategoryRequest request, CancellationToken ct);
    Task<SubCategoryDto?> UpdateAsync(int id, UpdateSubCategoryRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
