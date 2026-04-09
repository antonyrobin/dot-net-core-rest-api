using dot_net_core_rest_api.Data;
using dot_net_core_rest_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_rest_api.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<List<Category>> GetAllAsync(CancellationToken ct)
    {
        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public Task<Category> CreateAsync(Category category, CancellationToken ct)
    {
        db.Categories.Add(category);
        return Task.FromResult(category);
    }

    public Task UpdateAsync(Category category, CancellationToken ct)
    {
        db.Categories.Update(category);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null)
            return false;

        db.Categories.Remove(category);
        return true;
    }
}
