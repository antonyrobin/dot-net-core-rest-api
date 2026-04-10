using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Repositories;

namespace dot_net_core_rest_api.Services;

public class SubCategoryService(ISubCategoryRepository repository, ILogger<SubCategoryService> logger) : ISubCategoryService
{
    public async Task<PagedResult<SubCategoryDto>> GetAllAsync(SubCategoryQueryParameters query, CancellationToken ct)
    {
        logger.LogDebug("Service: Getting all sub-categories with query parameters");
        logger.LogTrace("Service: Query details – Page={Page}, Limit={Limit}, Cursor={Cursor}, Sort={Sort}, Name={Name}, Code={Code}, CategoryId={CategoryId}",
            query.Page, query.Limit, query.Cursor, query.Sort, query.Name, query.Code, query.CategoryId);

        var result = await repository.GetAllAsync(query, ct);

        logger.LogInformation("Retrieved {Count} sub-categories (Total: {Total})", result.Items.Count, result.Total);

        return new PagedResult<SubCategoryDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            Total = result.Total,
            Cursor = result.Cursor,
            HasMore = result.HasMore
        };
    }

    public async Task<SubCategoryDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Service: Getting sub-category by ID: {SubCategoryId}", id);

        var subCategory = await repository.GetByIdAsync(id, ct);

        if (subCategory is null)
        {
            logger.LogWarning("Sub-category not found: {SubCategoryId}", id);
            return null;
        }

        logger.LogDebug("Service: Found sub-category {SubCategoryId} ({SubCategoryName})", subCategory.Id, subCategory.Name);
        return ToDto(subCategory);
    }

    public async Task<SubCategoryDto> CreateAsync(CreateSubCategoryRequest request, CancellationToken ct)
    {
        logger.LogDebug("Service: Creating sub-category with Code={Code}, Name={Name}, CategoryId={CategoryId}",
            request.Code, request.Name, request.CategoryId);

        var subCategory = new SubCategory
        {
            Code = request.Code,
            Name = request.Name,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(subCategory, ct);

        logger.LogInformation("SubCategory created: {SubCategoryId} {SubCategoryCode}", subCategory.Id, subCategory.Code);

        return ToDto(subCategory);
    }

    public async Task<SubCategoryDto?> UpdateAsync(int id, UpdateSubCategoryRequest request, CancellationToken ct)
    {
        logger.LogDebug("Service: Updating sub-category {SubCategoryId}", id);

        var subCategory = await repository.GetByIdAsync(id, ct);
        if (subCategory is null)
        {
            logger.LogWarning("Sub-category not found for update: {SubCategoryId}", id);
            return null;
        }

        if (request.Code is not null)
            subCategory.Code = request.Code;

        if (request.Name is not null)
            subCategory.Name = request.Name;

        if (request.CategoryId is not null)
            subCategory.CategoryId = request.CategoryId.Value;

        await repository.UpdateAsync(subCategory, ct);

        logger.LogInformation("SubCategory updated: {SubCategoryId}", subCategory.Id);

        return ToDto(subCategory);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Service: Deleting sub-category {SubCategoryId}", id);

        var deleted = await repository.DeleteAsync(id, ct);

        if (deleted)
            logger.LogInformation("SubCategory deleted: {SubCategoryId}", id);
        else
            logger.LogWarning("Sub-category not found for deletion: {SubCategoryId}", id);

        return deleted;
    }

    private static SubCategoryDto ToDto(SubCategory s) => new(s.Id, s.CreatedAt, s.Code, s.Name, s.CategoryId);
}
