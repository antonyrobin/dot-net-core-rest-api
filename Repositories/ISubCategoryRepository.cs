using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Models;

namespace dot_net_core_rest_api.Repositories;

public interface ISubCategoryRepository
{
    Task<PagedResult<SubCategory>> GetAllAsync(SubCategoryQueryParameters query, CancellationToken ct);
    Task<SubCategory?> GetByIdAsync(int id, CancellationToken ct);
    Task<SubCategory> CreateAsync(SubCategory subCategory, CancellationToken ct);
    Task UpdateAsync(SubCategory subCategory, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
