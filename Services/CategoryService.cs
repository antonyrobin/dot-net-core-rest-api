using System.Text.Json;
using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Repositories;
using Microsoft.Extensions.Caching.Distributed;

namespace dot_net_core_rest_api.Services;

public class CategoryService(
    ICategoryRepository repository,
    IDistributedCache cache,
    IConfiguration config,
    ILogger<CategoryService> logger) : ICategoryService
{
    private static readonly string CachePrefix = "categories";
    private TimeSpan GetAllTtl => TimeSpan.FromSeconds(config.GetValue("Redis:CacheTtlSeconds:GetAll", 60));
    private TimeSpan GetByIdTtl => TimeSpan.FromSeconds(config.GetValue("Redis:CacheTtlSeconds:GetById", 120));

    public async Task<PagedResult<CategoryDto>> GetAllAsync(CategoryQueryParameters query, CancellationToken ct)
    {
        logger.LogDebug("Service: Getting all categories with query parameters");

        var cacheKey = $"{CachePrefix}:all:{query.Page}:{query.Limit}:{query.Cursor}:{query.Sort}:{query.Name}:{query.Code}";
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for key {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<PagedResult<CategoryDto>>(cached)!;
        }

        logger.LogDebug("Cache miss for key {CacheKey}", cacheKey);
        var result = await repository.GetAllAsync(query, ct);

        logger.LogInformation("Retrieved {Count} categories (Total: {Total})", result.Items.Count, result.Total);

        var dto = new PagedResult<CategoryDto>
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

    public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Service: Getting category by ID: {CategoryId}", id);

        var cacheKey = $"{CachePrefix}:{id}";
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for category {CategoryId}", id);
            return JsonSerializer.Deserialize<CategoryDto>(cached);
        }

        var category = await repository.GetByIdAsync(id, ct);

        if (category is null)
        {
            logger.LogWarning("Category not found: {CategoryId}", id);
            return null;
        }

        logger.LogDebug("Service: Found category {CategoryId} ({CategoryName})", category.Id, category.Name);
        var dto = ToDto(category);

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = GetByIdTtl }, ct);

        return dto;
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
        await InvalidateListCacheAsync(ct);

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
        await InvalidateEntityCacheAsync(id, ct);

        logger.LogInformation("Category updated: {CategoryId}", category.Id);

        return ToDto(category);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Service: Deleting category {CategoryId}", id);

        var deleted = await repository.DeleteAsync(id, ct);

        if (deleted)
        {
            await InvalidateEntityCacheAsync(id, ct);
            logger.LogInformation("Category deleted: {CategoryId}", id);
        }
        else
        {
            logger.LogWarning("Category not found for deletion: {CategoryId}", id);
        }

        return deleted;
    }

    private async Task InvalidateListCacheAsync(CancellationToken ct)
    {
        // Remove all list caches by removing the known pattern prefix
        // IDistributedCache doesn't support pattern delete; remove the entity + rely on TTL for lists
        logger.LogDebug("Invalidating category list cache");
        await cache.RemoveAsync($"{CachePrefix}:all:", ct);
    }

    private async Task InvalidateEntityCacheAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Invalidating cache for category {CategoryId}", id);
        await cache.RemoveAsync($"{CachePrefix}:{id}", ct);
        await InvalidateListCacheAsync(ct);
    }

    private static CategoryDto ToDto(Category c) => new(c.Id, c.CreatedAt, c.Code, c.Name);
}
