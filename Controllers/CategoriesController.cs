using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace dot_net_core_rest_api.Controllers;

[Route("api/v1/categories")]
[Authorize]
public class CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger) : BaseApiController
{
    /// <summary>
    /// Get all categories with pagination, filtering, and sorting.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "CachePublicGet")]
    public async Task<IActionResult> GetAll([FromQuery] CategoryQueryParameters query, CancellationToken ct)
    {
        logger.LogDebug("GetAll categories requested | RequestId: {RequestId}", GetRequestId());
        logger.LogDebug("Query: Page={Page}, Limit={Limit}, Sort={Sort}, Name={Name}, Code={Code}",
            query.Page, query.Limit, query.Sort, query.Name, query.Code);

        var result = await categoryService.GetAllAsync(query, ct);

        logger.LogDebug("GetAll categories completed: {Count} items returned | RequestId: {RequestId}",
            result.Items.Count, GetRequestId());

        return ApiOk(result.Items, new PaginationMeta
        {
            Page = query.Page ?? 1,
            Limit = query.Limit ?? 20,
            Total = result.Total,
            Cursor = result.Cursor,
            HasMore = result.HasMore
        });
    }

    /// <summary>
    /// Get a category by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "CachePublicGet")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        logger.LogDebug("GetById category {CategoryId} requested | RequestId: {RequestId}", id, GetRequestId());

        var category = await categoryService.GetByIdAsync(id, ct);

        if (category is null)
        {
            logger.LogWarning("Category {CategoryId} not found | RequestId: {RequestId}", id, GetRequestId());
            return ApiNotFound($"Category with id {id} was not found.");
        }

        logger.LogDebug("GetById category {CategoryId} completed | RequestId: {RequestId}", id, GetRequestId());
        return ApiOk(category);
    }

    /// <summary>
    /// Create a new category.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        logger.LogDebug("Create category requested: Code={Code} | RequestId: {RequestId}",
            request.Code, GetRequestId());

        var category = await categoryService.CreateAsync(request, ct);

        logger.LogDebug("Category created: {CategoryId} | RequestId: {RequestId}", category.Id, GetRequestId());
        return ApiCreated(nameof(GetById), new { id = category.Id }, category);
    }

    /// <summary>
    /// Update an existing category.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        logger.LogDebug("Update category {CategoryId} requested | RequestId: {RequestId}", id, GetRequestId());

        var category = await categoryService.UpdateAsync(id, request, ct);

        if (category is null)
        {
            logger.LogWarning("Category {CategoryId} not found for update | RequestId: {RequestId}", id, GetRequestId());
            return ApiNotFound($"Category with id {id} was not found.");
        }

        logger.LogDebug("Category {CategoryId} updated | RequestId: {RequestId}", id, GetRequestId());
        return ApiOk(category);
    }

    /// <summary>
    /// Delete a category by id.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        logger.LogDebug("Delete category {CategoryId} requested | RequestId: {RequestId}", id, GetRequestId());

        var deleted = await categoryService.DeleteAsync(id, ct);

        if (!deleted)
        {
            logger.LogWarning("Category {CategoryId} not found for deletion | RequestId: {RequestId}", id, GetRequestId());
            return ApiNotFound($"Category with id {id} was not found.");
        }

        logger.LogDebug("Category {CategoryId} deleted | RequestId: {RequestId}", id, GetRequestId());
        return NoContent();
    }
}
