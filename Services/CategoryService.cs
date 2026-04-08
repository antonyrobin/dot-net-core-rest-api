using dot_net_core_rest_api.Data;
using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_rest_api.Services;

public class CategoryService(AppDbContext db, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken ct)
    {
        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => ToDto(c))
            .ToListAsync(ct);
    }

    public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return category is null ? null : ToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = new Category
        {
            Code = request.Code,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Category created: {CategoryId} {CategoryCode}", category.Id, category.Code);

        return ToDto(category);
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null)
            return null;

        if (request.Code is not null)
            category.Code = request.Code;

        if (request.Name is not null)
            category.Name = request.Name;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Category updated: {CategoryId}", category.Id);

        return ToDto(category);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null)
            return false;

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Category deleted: {CategoryId}", category.Id);

        return true;
    }

    private static CategoryDto ToDto(Category c) => new(c.Id, c.CreatedAt, c.Code, c.Name);
}
