using dot_net_core_rest_api.Entities;

namespace dot_net_core_rest_api.Repositories;

public interface ISubCategoryRepository
{
    Task<List<SubCategory>> GetAllAsync(CancellationToken ct);
    Task<List<SubCategory>> GetByCategoryIdAsync(int categoryId, CancellationToken ct);
    Task<SubCategory?> GetByIdAsync(int id, CancellationToken ct);
    Task<SubCategory> CreateAsync(SubCategory subCategory, CancellationToken ct);
    Task UpdateAsync(SubCategory subCategory, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
