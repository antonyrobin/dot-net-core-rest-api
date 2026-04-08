using dot_net_core_rest_api.Entities;

namespace dot_net_core_rest_api.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(CancellationToken ct);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct);
    Task<Category> CreateAsync(Category category, CancellationToken ct);
    Task UpdateAsync(Category category, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
