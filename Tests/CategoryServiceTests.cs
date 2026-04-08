using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Repositories;
using dot_net_core_rest_api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace dot_net_core_rest_api.Tests;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repoMock = new();
    private readonly Mock<ILogger<CategoryService>> _loggerMock = new();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new CategoryService(_repoMock.Object, _loggerMock.Object);
    }

    // ───── GetAllAsync ─────

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var entities = new List<Category>
        {
            new() { Id = 1, CreatedAt = DateTime.UtcNow, Code = "CAT1", Name = "Alpha" },
            new() { Id = 2, CreatedAt = DateTime.UtcNow, Code = "CAT2", Name = "Beta" }
        };
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _service.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("CAT1", result[0].Code);
        Assert.Equal("Beta", result[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.GetAllAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    // ───── GetByIdAsync ─────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new Category { Id = 1, CreatedAt = DateTime.UtcNow, Code = "CAT1", Name = "Alpha" };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("CAT1", result!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        Assert.Null(result);
    }

    // ───── CreateAsync ─────

    [Fact]
    public async Task CreateAsync_CallsRepositoryAndReturnsDto()
    {
        var request = new CreateCategoryRequest("NEW", "New Category");
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category c, CancellationToken _) => { c.Id = 10; return c; });

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(10, result.Id);
        Assert.Equal("NEW", result.Code);
        Assert.Equal("New Category", result.Name);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ───── UpdateAsync ─────

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesBothFields()
    {
        var entity = new Category { Id = 1, CreatedAt = DateTime.UtcNow, Code = "OLD", Name = "Old Name" };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var request = new UpdateCategoryRequest("UPD", "Updated Name");
        var result = await _service.UpdateAsync(1, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("UPD", result!.Code);
        Assert.Equal("Updated Name", result.Name);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesOnlyCode()
    {
        var entity = new Category { Id = 1, CreatedAt = DateTime.UtcNow, Code = "OLD", Name = "Keep This" };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var request = new UpdateCategoryRequest("NEW", null);
        var result = await _service.UpdateAsync(1, request, CancellationToken.None);

        Assert.Equal("NEW", result!.Code);
        Assert.Equal("Keep This", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesOnlyName()
    {
        var entity = new Category { Id = 1, CreatedAt = DateTime.UtcNow, Code = "KEEP", Name = "Old Name" };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var request = new UpdateCategoryRequest(null, "New Name");
        var result = await _service.UpdateAsync(1, request, CancellationToken.None);

        Assert.Equal("KEEP", result!.Code);
        Assert.Equal("New Name", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var result = await _service.UpdateAsync(99, new UpdateCategoryRequest("X", "Y"), CancellationToken.None);

        Assert.Null(result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
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
