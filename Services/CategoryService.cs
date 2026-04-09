using dot_net_core_rest_api.Data;
using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Repositories;

namespace dot_net_core_rest_api.Services;

public class CategoryService(ICategoryRepository repository, IUnitOfWork unitOfWork, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken ct)
    {
        var categories = await repository.GetAllAsync(ct);
        return categories.Select(ToDto).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(id, ct);
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

        await repository.CreateAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Category created: {CategoryId} {CategoryCode}", category.Id, category.Code);

        return ToDto(category);
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(id, ct);
        if (category is null)
            return null;

        if (request.Code is not null)
            category.Code = request.Code;

        if (request.Name is not null)
            category.Name = request.Name;

        await repository.UpdateAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Category updated: {CategoryId}", category.Id);

        return ToDto(category);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);

        if (deleted)
        {
            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation("Category deleted: {CategoryId}", id);
        }

        return deleted;
    }

    private static CategoryDto ToDto(Category c) => new(c.Id, c.CreatedAt, c.Code, c.Name);
}
