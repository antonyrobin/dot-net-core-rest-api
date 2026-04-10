using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Repositories;

namespace dot_net_core_rest_api.Services;

public class CategoryService(ICategoryRepository repository, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<PagedResult<CategoryDto>> GetAllAsync(CategoryQueryParameters query, CancellationToken ct)
    {
        logger.LogDebug("Service: Getting all categories with query parameters");
        logger.LogTrace("Service: Query details – Page={Page}, Limit={Limit}, Cursor={Cursor}, Sort={Sort}, Name={Name}, Code={Code}",
            query.Page, query.Limit, query.Cursor, query.Sort, query.Name, query.Code);

        var result = await repository.GetAllAsync(query, ct);

        logger.LogInformation("Retrieved {Count} categories (Total: {Total})", result.Items.Count, result.Total);

        return new PagedResult<CategoryDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            Total = result.Total,
            Cursor = result.Cursor,
            HasMore = result.HasMore
        };
    }

    public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Service: Getting category by ID: {CategoryId}", id);

        var category = await repository.GetByIdAsync(id, ct);

        if (category is null)
        {
            logger.LogWarning("Category not found: {CategoryId}", id);
            return null;
        }

        logger.LogDebug("Service: Found category {CategoryId} ({CategoryName})", category.Id, category.Name);
        return ToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        logger.LogDebug("Service: Creating category with Code={Code}, Name={Name}", request.Code, request.Name);

        var category = new Category
        {
            Code = request.Code,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(category, ct);

        logger.LogInformation("Category created: {CategoryId} {CategoryCode}", category.Id, category.Code);

        return ToDto(category);
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        logger.LogDebug("Service: Updating category {CategoryId}", id);

        var category = await repository.GetByIdAsync(id, ct);
        if (category is null)
        {
            logger.LogWarning("Category not found for update: {CategoryId}", id);
            return null;
        }

        if (request.Code is not null)
            category.Code = request.Code;

        if (request.Name is not null)
            category.Name = request.Name;

        await repository.UpdateAsync(category, ct);

        logger.LogInformation("Category updated: {CategoryId}", category.Id);

        return ToDto(category);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Service: Deleting category {CategoryId}", id);

        var deleted = await repository.DeleteAsync(id, ct);

        if (deleted)
            logger.LogInformation("Category deleted: {CategoryId}", id);
        else
            logger.LogWarning("Category not found for deletion: {CategoryId}", id);

        return deleted;
    }

    private static CategoryDto ToDto(Category c) => new(c.Id, c.CreatedAt, c.Code, c.Name);
}
