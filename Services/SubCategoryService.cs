using System.Text.Json;
using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Repositories;
using Microsoft.Extensions.Caching.Distributed;

namespace dot_net_core_rest_api.Services;

public class SubCategoryService(
    ISubCategoryRepository repository,
    IDistributedCache cache,
    IConfiguration config,
    ILogger<SubCategoryService> logger) : ISubCategoryService
{
    private static readonly string CachePrefix = "subcategories";
    private TimeSpan GetAllTtl => TimeSpan.FromSeconds(config.GetValue("Redis:CacheTtlSeconds:GetAll", 60));
    private TimeSpan GetByIdTtl => TimeSpan.FromSeconds(config.GetValue("Redis:CacheTtlSeconds:GetById", 120));

    public async Task<PagedResult<SubCategoryDto>> GetAllAsync(SubCategoryQueryParameters query, CancellationToken ct)
    {
        logger.LogDebug("Service: Getting all sub-categories with query parameters");

        var cacheKey = $"{CachePrefix}:all:{query.Page}:{query.Limit}:{query.Cursor}:{query.Sort}:{query.Name}:{query.Code}:{query.CategoryId}";
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for key {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<PagedResult<SubCategoryDto>>(cached)!;
        }

        logger.LogDebug("Cache miss for key {CacheKey}", cacheKey);
        var result = await repository.GetAllAsync(query, ct);

        logger.LogInformation("Retrieved {Count} sub-categories (Total: {Total})", result.Items.Count, result.Total);

        var dto = new PagedResult<SubCategoryDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            Total = result.Total,
            Cursor = result.Cursor,
            HasMore = result.HasMore
        };

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = GetAllTtl }, ct);

        return dto;
    }

    public async Task<SubCategoryDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Service: Getting sub-category by ID: {SubCategoryId}", id);

        var cacheKey = $"{CachePrefix}:{id}";
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for sub-category {SubCategoryId}", id);
            return JsonSerializer.Deserialize<SubCategoryDto>(cached);
        }

        var subCategory = await repository.GetByIdAsync(id, ct);

        if (subCategory is null)
        {
            logger.LogWarning("Sub-category not found: {SubCategoryId}", id);
            return null;
        }

        logger.LogDebug("Service: Found sub-category {SubCategoryId} ({SubCategoryName})", subCategory.Id, subCategory.Name);
        var dto = ToDto(subCategory);

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = GetByIdTtl }, ct);

        return dto;
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
        await InvalidateListCacheAsync(ct);

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
        await InvalidateEntityCacheAsync(id, ct);

        logger.LogInformation("SubCategory updated: {SubCategoryId}", subCategory.Id);

        return ToDto(subCategory);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Service: Deleting sub-category {SubCategoryId}", id);

        var deleted = await repository.DeleteAsync(id, ct);

        if (deleted)
        {
            await InvalidateEntityCacheAsync(id, ct);
            logger.LogInformation("SubCategory deleted: {SubCategoryId}", id);
        }
        else
        {
            logger.LogWarning("Sub-category not found for deletion: {SubCategoryId}", id);
        }

        return deleted;
    }

    private async Task InvalidateListCacheAsync(CancellationToken ct)
    {
        logger.LogDebug("Invalidating sub-category list cache");
        await cache.RemoveAsync($"{CachePrefix}:all:", ct);
    }

    private async Task InvalidateEntityCacheAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Invalidating cache for sub-category {SubCategoryId}", id);
        await cache.RemoveAsync($"{CachePrefix}:{id}", ct);
        await InvalidateListCacheAsync(ct);
    }

    private static SubCategoryDto ToDto(SubCategory s) => new(s.Id, s.CreatedAt, s.Code, s.Name, s.CategoryId);
}
