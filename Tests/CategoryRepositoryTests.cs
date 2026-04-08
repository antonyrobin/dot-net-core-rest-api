using dot_net_core_rest_api.Data;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_rest_api.Tests;

public class CategoryRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CategoryRepository _repository;

    public CategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _repository = new CategoryRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedAsync(params Category[] categories)
    {
        _db.Categories.AddRange(categories);
        await _db.SaveChangesAsync();
    }

    // ───── GetAllAsync ─────

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrderedByName()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var result = await _repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Bravo", result[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _repository.GetAllAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    // ───── GetByIdAsync ─────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsCategory()
    {
        await SeedAsync(new Category { Id = 1, Code = "CAT1", Name = "Test", CreatedAt = DateTime.UtcNow });

        var result = await _repository.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("CAT1", result!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(99, CancellationToken.None);

        Assert.Null(result);
    }

    // ───── CreateAsync ─────

    [Fact]
    public async Task CreateAsync_AddsAndReturnsCategory()
    {
        var category = new Category { Code = "NEW", Name = "New Category", CreatedAt = DateTime.UtcNow };

        var result = await _repository.CreateAsync(category, CancellationToken.None);

        Assert.True(result.Id > 0);
        Assert.Equal("NEW", result.Code);
        Assert.Single(_db.Categories);
    }

    // ───── UpdateAsync ─────

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await SeedAsync(new Category { Id = 1, Code = "OLD", Name = "Old Name", CreatedAt = DateTime.UtcNow });

        // Detach the tracked entity so Update can re-attach
        _db.ChangeTracker.Clear();

        var updated = new Category { Id = 1, Code = "UPD", Name = "Updated", CreatedAt = DateTime.UtcNow };
        await _repository.UpdateAsync(updated, CancellationToken.None);

        var fromDb = await _db.Categories.FindAsync(1);
        Assert.Equal("UPD", fromDb!.Code);
        Assert.Equal("Updated", fromDb.Name);
    }

    // ───── DeleteAsync ─────

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesAndReturnsTrue()
    {
        await SeedAsync(new Category { Id = 1, Code = "DEL", Name = "Delete Me", CreatedAt = DateTime.UtcNow });

        var result = await _repository.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
        Assert.Empty(_db.Categories);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(99, CancellationToken.None);

        Assert.False(result);
    }
}
