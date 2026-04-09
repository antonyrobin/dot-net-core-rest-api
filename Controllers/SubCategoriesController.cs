using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dot_net_core_rest_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubCategoriesController(ISubCategoryService subCategoryService) : ControllerBase
{
    /// <summary>
    /// Get all sub-categories.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<SubCategoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var subCategories = await subCategoryService.GetAllAsync(ct);
        return Ok(subCategories);
    }

    /// <summary>
    /// Get all sub-categories for a given category.
    /// </summary>
    [HttpGet("by-category/{categoryId:int}")]
    [AllowAnonymous]
    [ProducesResponseType<List<SubCategoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategoryId(int categoryId, CancellationToken ct)
    {
        var subCategories = await subCategoryService.GetByCategoryIdAsync(categoryId, ct);
        return Ok(subCategories);
    }

    /// <summary>
    /// Get a sub-category by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<SubCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var subCategory = await subCategoryService.GetByIdAsync(id, ct);
        return subCategory is null ? NotFound() : Ok(subCategory);
    }

    /// <summary>
    /// Create a new sub-category.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SubCategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateSubCategoryRequest request, CancellationToken ct)
    {
        var subCategory = await subCategoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = subCategory.Id }, subCategory);
    }

    /// <summary>
    /// Update an existing sub-category.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<SubCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, UpdateSubCategoryRequest request, CancellationToken ct)
    {
        var subCategory = await subCategoryService.UpdateAsync(id, request, ct);
        return subCategory is null ? NotFound() : Ok(subCategory);
    }

    /// <summary>
    /// Delete a sub-category by id.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await subCategoryService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
