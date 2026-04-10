using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace dot_net_core_rest_api.Controllers;

[Route("api/v1/sub-categories")]
[Authorize]
public class SubCategoriesController(ISubCategoryService subCategoryService, ILogger<SubCategoriesController> logger) : BaseApiController
{
    /// <summary>
    /// Get all sub-categories with pagination, filtering, and sorting.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "CachePublicGet")]
    public async Task<IActionResult> GetAll([FromQuery] SubCategoryQueryParameters query, CancellationToken ct)
    {
        logger.LogDebug("GetAll sub-categories requested | RequestId: {RequestId}", GetRequestId());
        logger.LogDebug("Query: Page={Page}, Limit={Limit}, Sort={Sort}, Name={Name}, Code={Code}, CategoryId={CategoryId}",
            query.Page, query.Limit, query.Sort, query.Name, query.Code, query.CategoryId);

        var result = await subCategoryService.GetAllAsync(query, ct);

        logger.LogDebug("GetAll sub-categories completed: {Count} items returned | RequestId: {RequestId}",
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
    /// Get all sub-categories for a given category.
    /// </summary>
    [HttpGet("by-category/{categoryId:int}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "CachePublicGet")]
    public async Task<IActionResult> GetByCategoryId(int categoryId, [FromQuery] SubCategoryQueryParameters query, CancellationToken ct)
    {
        logger.LogDebug("GetByCategoryId {CategoryId} requested | RequestId: {RequestId}", categoryId, GetRequestId());

        query.CategoryId = categoryId;
        var result = await subCategoryService.GetAllAsync(query, ct);

        logger.LogDebug("GetByCategoryId {CategoryId} completed: {Count} items returned | RequestId: {RequestId}",
            categoryId, result.Items.Count, GetRequestId());

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
    /// Get a sub-category by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "CachePublicGet")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        logger.LogDebug("GetById sub-category {SubCategoryId} requested | RequestId: {RequestId}", id, GetRequestId());

        var subCategory = await subCategoryService.GetByIdAsync(id, ct);

        if (subCategory is null)
        {
            logger.LogWarning("Sub-category {SubCategoryId} not found | RequestId: {RequestId}", id, GetRequestId());
            return ApiNotFound($"Sub-category with id {id} was not found.");
        }

        logger.LogDebug("GetById sub-category {SubCategoryId} completed | RequestId: {RequestId}", id, GetRequestId());
        return ApiOk(subCategory);
    }

    /// <summary>
    /// Create a new sub-category.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateSubCategoryRequest request, CancellationToken ct)
    {
        logger.LogDebug("Create sub-category requested: Code={Code} | RequestId: {RequestId}",
            request.Code, GetRequestId());

        var subCategory = await subCategoryService.CreateAsync(request, ct);

        logger.LogDebug("Sub-category created: {SubCategoryId} | RequestId: {RequestId}", subCategory.Id, GetRequestId());
        return ApiCreated(nameof(GetById), new { id = subCategory.Id }, subCategory);
    }

    /// <summary>
    /// Update an existing sub-category.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateSubCategoryRequest request, CancellationToken ct)
    {
        logger.LogDebug("Update sub-category {SubCategoryId} requested | RequestId: {RequestId}", id, GetRequestId());

        var subCategory = await subCategoryService.UpdateAsync(id, request, ct);

        if (subCategory is null)
        {
            logger.LogWarning("Sub-category {SubCategoryId} not found for update | RequestId: {RequestId}", id, GetRequestId());
            return ApiNotFound($"Sub-category with id {id} was not found.");
        }

        logger.LogDebug("Sub-category {SubCategoryId} updated | RequestId: {RequestId}", id, GetRequestId());
        return ApiOk(subCategory);
    }

    /// <summary>
    /// Delete a sub-category by id.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        logger.LogDebug("Delete sub-category {SubCategoryId} requested | RequestId: {RequestId}", id, GetRequestId());

        var deleted = await subCategoryService.DeleteAsync(id, ct);

        if (!deleted)
        {
            logger.LogWarning("Sub-category {SubCategoryId} not found for deletion | RequestId: {RequestId}", id, GetRequestId());
            return ApiNotFound($"Sub-category with id {id} was not found.");
        }

        logger.LogDebug("Sub-category {SubCategoryId} deleted | RequestId: {RequestId}", id, GetRequestId());
        return NoContent();
    }
}
