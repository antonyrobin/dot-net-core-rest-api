using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Models;

namespace dot_net_core_rest_api.Services;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetAllAsync(CategoryQueryParameters query, CancellationToken ct);
    Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
