using dot_net_core_rest_api.Controllers;
using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace dot_net_core_rest_api.Tests;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _serviceMock = new();
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _controller = new CategoriesController(_serviceMock.Object);
    }

    // ───── GetAll ─────

    [Fact]
    public async Task GetAll_ReturnsOkWithCategories()
    {
        var categories = new List<CategoryDto>
        {
            new(1, DateTime.UtcNow, "CAT1", "Category 1"),
            new(2, DateTime.UtcNow, "CAT2", "Category 2")
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(categories, okResult.Value);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithEmptyList()
    {
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Empty((List<CategoryDto>)okResult.Value!);
    }

    // ───── GetById ─────

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var dto = new CategoryDto(1, DateTime.UtcNow, "CAT1", "Category 1");
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryDto?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ───── Create ─────

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var request = new CreateCategoryRequest("CAT1", "Category 1");
        var dto = new CategoryDto(1, DateTime.UtcNow, "CAT1", "Category 1");
        _serviceMock.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(CategoriesController.GetById), createdResult.ActionName);
        Assert.Equal(1, createdResult.RouteValues!["id"]);
        Assert.Equal(dto, createdResult.Value);
    }

    // ───── Update ─────

    [Fact]
    public async Task Update_ExistingId_ReturnsOk()
    {
        var request = new UpdateCategoryRequest("UPD", "Updated");
        var dto = new CategoryDto(1, DateTime.UtcNow, "UPD", "Updated");
        _serviceMock.Setup(s => s.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Update(1, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var request = new UpdateCategoryRequest("UPD", "Updated");
        _serviceMock.Setup(s => s.UpdateAsync(99, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryDto?)null);

        var result = await _controller.Update(99, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ───── Delete ─────

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Delete(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
