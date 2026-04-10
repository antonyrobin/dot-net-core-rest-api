using dot_net_core_rest_api.Controllers;
using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace dot_net_core_rest_api.Tests;

public class SubCategoriesControllerTests
{
    private readonly Mock<ISubCategoryService> _serviceMock = new();
    private readonly Mock<ILogger<SubCategoriesController>> _loggerMock = new();
    private readonly SubCategoriesController _controller;

    public SubCategoriesControllerTests()
    {
        _controller = new SubCategoriesController(_serviceMock.Object, _loggerMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // ───── GetAll ─────

    [Fact]
    public async Task GetAll_ReturnsOkWithSubCategories()
    {
        var subCategories = new List<SubCategoryDto>
        {
            new(1, DateTime.UtcNow, "SUB1", "SubCategory 1", 10),
            new(2, DateTime.UtcNow, "SUB2", "SubCategory 2", 10)
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<SubCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SubCategoryDto> { Items = subCategories, Total = 2, HasMore = false });

        var result = await _controller.GetAll(new SubCategoryQueryParameters(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<List<SubCategoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data!.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithEmptyList()
    {
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<SubCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SubCategoryDto> { Items = [], Total = 0, HasMore = false });

        var result = await _controller.GetAll(new SubCategoryQueryParameters(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<List<SubCategoryDto>>>(okResult.Value);
        Assert.Empty(response.Data!);
    }

    // ───── GetByCategoryId ─────

    [Fact]
    public async Task GetByCategoryId_ReturnsOkWithSubCategories()
    {
        var subCategories = new List<SubCategoryDto>
        {
            new(1, DateTime.UtcNow, "SUB1", "SubCategory 1", 5)
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<SubCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SubCategoryDto> { Items = subCategories, Total = 1, HasMore = false });

        var result = await _controller.GetByCategoryId(5, new SubCategoryQueryParameters(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<List<SubCategoryDto>>>(okResult.Value);
        Assert.Single(response.Data!);
    }

    // ───── GetById ─────

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var dto = new SubCategoryDto(1, DateTime.UtcNow, "SUB1", "SubCategory 1", 10);
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<SubCategoryDto>>(okResult.Value);
        Assert.Equal(dto, response.Data);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubCategoryDto?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ───── Create ─────

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var request = new CreateSubCategoryRequest("SUB1", "SubCategory 1", 10);
        var dto = new SubCategoryDto(1, DateTime.UtcNow, "SUB1", "SubCategory 1", 10);
        _serviceMock.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(SubCategoriesController.GetById), createdResult.ActionName);
        Assert.Equal(1, createdResult.RouteValues!["id"]);
        var response = Assert.IsType<ApiSuccessResponse<SubCategoryDto>>(createdResult.Value);
        Assert.Equal(dto, response.Data);
    }

    // ───── Update ─────

    [Fact]
    public async Task Update_ExistingId_ReturnsOk()
    {
        var request = new UpdateSubCategoryRequest("UPD", "Updated", null);
        var dto = new SubCategoryDto(1, DateTime.UtcNow, "UPD", "Updated", 10);
        _serviceMock.Setup(s => s.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Update(1, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<SubCategoryDto>>(okResult.Value);
        Assert.Equal(dto, response.Data);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var request = new UpdateSubCategoryRequest("UPD", "Updated", null);
        _serviceMock.Setup(s => s.UpdateAsync(99, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubCategoryDto?)null);

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
