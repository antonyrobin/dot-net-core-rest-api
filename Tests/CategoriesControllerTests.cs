using dot_net_core_rest_api.Controllers;
using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace dot_net_core_rest_api.Tests;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _serviceMock = new();
    private readonly Mock<ILogger<CategoriesController>> _loggerMock = new();
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _controller = new CategoriesController(_serviceMock.Object, _loggerMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
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
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CategoryDto> { Items = categories, Total = 2, HasMore = false });

        var result = await _controller.GetAll(new CategoryQueryParameters(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<List<CategoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data!.Count);
        Assert.NotNull(response.Meta);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithEmptyList()
    {
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CategoryDto> { Items = [], Total = 0, HasMore = false });

        var result = await _controller.GetAll(new CategoryQueryParameters(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<List<CategoryDto>>>(okResult.Value);
        Assert.Empty(response.Data!);
    }

    [Fact]
    public async Task GetAll_SetsMetaCorrectly()
    {
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CategoryDto>
            {
                Items = [new(1, DateTime.UtcNow, "A", "Alpha")],
                Total = 50,
                Cursor = "abc",
                HasMore = true
            });

        var query = new CategoryQueryParameters { Page = 3, Limit = 10 };
        var result = await _controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<List<CategoryDto>>>(okResult.Value);
        Assert.Equal(3, response.Meta!.Page);
        Assert.Equal(10, response.Meta.Limit);
        Assert.Equal(50, response.Meta.Total);
        Assert.Equal("abc", response.Meta.Cursor);
        Assert.True(response.Meta.HasMore);
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
        var response = Assert.IsType<ApiSuccessResponse<CategoryDto>>(okResult.Value);
        Assert.Equal(dto, response.Data);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryDto?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
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
        var response = Assert.IsType<ApiSuccessResponse<CategoryDto>>(createdResult.Value);
        Assert.Equal(dto, response.Data);
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
        var response = Assert.IsType<ApiSuccessResponse<CategoryDto>>(okResult.Value);
        Assert.Equal(dto, response.Data);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var request = new UpdateCategoryRequest("UPD", "Updated");
        _serviceMock.Setup(s => s.UpdateAsync(99, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryDto?)null);

        var result = await _controller.Update(99, request, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
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

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
