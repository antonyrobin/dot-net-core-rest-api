using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dot_net_core_rest_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    /// <summary>
    /// Get all categories.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var categories = await categoryService.GetAllAsync(ct);
        return Ok(categories);
    }

    /// <summary>
    /// Get a category by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var category = await categoryService.GetByIdAsync(id, ct);
        return category is null ? NotFound() : Ok(category);
    }

    /// <summary>
    /// Create a new category.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    /// <summary>
    /// Update an existing category.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.UpdateAsync(id, request, ct);
        return category is null ? NotFound() : Ok(category);
    }

    /// <summary>
    /// Delete a category by id.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await categoryService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
