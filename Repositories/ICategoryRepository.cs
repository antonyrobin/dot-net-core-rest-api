using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Models;

namespace dot_net_core_rest_api.Repositories;

public interface ICategoryRepository
{
    Task<PagedResult<Category>> GetAllAsync(CategoryQueryParameters query, CancellationToken ct);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct);
    Task<Category> CreateAsync(Category category, CancellationToken ct);
    Task UpdateAsync(Category category, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
