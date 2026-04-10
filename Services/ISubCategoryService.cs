using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Models;

namespace dot_net_core_rest_api.Services;

public interface ISubCategoryService
{
    Task<PagedResult<SubCategoryDto>> GetAllAsync(SubCategoryQueryParameters query, CancellationToken ct);
    Task<SubCategoryDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<SubCategoryDto> CreateAsync(CreateSubCategoryRequest request, CancellationToken ct);
    Task<SubCategoryDto?> UpdateAsync(int id, UpdateSubCategoryRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
