using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Repositories;
using dot_net_core_rest_api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace dot_net_core_rest_api.Tests;

public class SubCategoryServiceTests
{
    private readonly Mock<ISubCategoryRepository> _repoMock = new();
    private readonly Mock<ILogger<SubCategoryService>> _loggerMock = new();
    private readonly SubCategoryService _service;

    public SubCategoryServiceTests()
    {
        _service = new SubCategoryService(_repoMock.Object, _loggerMock.Object);
    }

    // ───── GetAllAsync ─────

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var entities = new List<SubCategory>
        {
            new() { Id = 1, CreatedAt = DateTime.UtcNow, Code = "SUB1", Name = "Alpha", CategoryId = 10 },
            new() { Id = 2, CreatedAt = DateTime.UtcNow, Code = "SUB2", Name = "Beta", CategoryId = 10 }
        };
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<SubCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SubCategory> { Items = entities, Total = 2, HasMore = false });

        var result = await _service.GetAllAsync(new SubCategoryQueryParameters(), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("SUB1", result.Items[0].Code);
        Assert.Equal("Beta", result.Items[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<SubCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SubCategory> { Items = [], Total = 0, HasMore = false });

        var result = await _service.GetAllAsync(new SubCategoryQueryParameters(), CancellationToken.None);

        Assert.Empty(result.Items);
    }

    // ───── GetAllAsync with CategoryId filter ─────

    [Fact]
    public async Task GetAllAsync_WithCategoryId_ReturnsMappedDtos()
    {
        var entities = new List<SubCategory>
        {
            new() { Id = 1, CreatedAt = DateTime.UtcNow, Code = "SUB1", Name = "Alpha", CategoryId = 5 }
        };
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<SubCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SubCategory> { Items = entities, Total = 1, HasMore = false });

        var result = await _service.GetAllAsync(new SubCategoryQueryParameters { CategoryId = 5 }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].CategoryId);
    }

    // ───── GetByIdAsync ─────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new SubCategory { Id = 1, CreatedAt = DateTime.UtcNow, Code = "SUB1", Name = "Alpha", CategoryId = 10 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("SUB1", result!.Code);
        Assert.Equal(10, result.CategoryId);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubCategory?)null);

        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        Assert.Null(result);
    }

    // ───── CreateAsync ─────

    [Fact]
    public async Task CreateAsync_CallsRepositoryAndReturnsDto()
    {
        var request = new CreateSubCategoryRequest("NEW", "New SubCategory", 10);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<SubCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubCategory s, CancellationToken _) => { s.Id = 10; return s; });

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(10, result.Id);
        Assert.Equal("NEW", result.Code);
        Assert.Equal("New SubCategory", result.Name);
        Assert.Equal(10, result.CategoryId);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<SubCategory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ───── UpdateAsync ─────

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesAllFields()
    {
        var entity = new SubCategory { Id = 1, CreatedAt = DateTime.UtcNow, Code = "OLD", Name = "Old Name", CategoryId = 10 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var request = new UpdateSubCategoryRequest("UPD", "Updated Name", 20);
        var result = await _service.UpdateAsync(1, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("UPD", result!.Code);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal(20, result.CategoryId);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<SubCategory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesOnlyCode()
    {
        var entity = new SubCategory { Id = 1, CreatedAt = DateTime.UtcNow, Code = "OLD", Name = "Keep This", CategoryId = 10 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var request = new UpdateSubCategoryRequest("NEW", null, null);
        var result = await _service.UpdateAsync(1, request, CancellationToken.None);

        Assert.Equal("NEW", result!.Code);
        Assert.Equal("Keep This", result.Name);
        Assert.Equal(10, result.CategoryId);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesOnlyName()
    {
        var entity = new SubCategory { Id = 1, CreatedAt = DateTime.UtcNow, Code = "KEEP", Name = "Old Name", CategoryId = 10 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var request = new UpdateSubCategoryRequest(null, "New Name", null);
        var result = await _service.UpdateAsync(1, request, CancellationToken.None);

        Assert.Equal("KEEP", result!.Code);
        Assert.Equal("New Name", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesOnlyCategoryId()
    {
        var entity = new SubCategory { Id = 1, CreatedAt = DateTime.UtcNow, Code = "KEEP", Name = "Keep Name", CategoryId = 10 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var request = new UpdateSubCategoryRequest(null, null, 99);
        var result = await _service.UpdateAsync(1, request, CancellationToken.None);

        Assert.Equal("KEEP", result!.Code);
        Assert.Equal("Keep Name", result.Name);
        Assert.Equal(99, result.CategoryId);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubCategory?)null);

        var result = await _service.UpdateAsync(99, new UpdateSubCategoryRequest("X", "Y", null), CancellationToken.None);

        Assert.Null(result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<SubCategory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ───── DeleteAsync ─────

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        _repoMock.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.DeleteAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.DeleteAsync(99, CancellationToken.None);

        Assert.False(result);
    }
}
