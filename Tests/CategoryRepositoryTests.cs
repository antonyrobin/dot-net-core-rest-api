using dot_net_core_rest_api.Data;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

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
        _repository = new CategoryRepository(_db, new Mock<ILogger<CategoryRepository>>().Object);
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

        var result = await _repository.GetAllAsync(new CategoryQueryParameters(), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Alpha", result.Items[0].Name);
        Assert.Equal("Bravo", result.Items[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _repository.GetAllAsync(new CategoryQueryParameters(), CancellationToken.None);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithNameFilter_ReturnsMatchingOnly()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Code = "C", Name = "Charlie Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Name = "Alpha" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, c => Assert.Contains("Alpha", c.Name));
    }

    [Fact]
    public async Task GetAllAsync_WithCodeFilter_ReturnsMatchingOnly()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "ELEC", Name = "Electronics", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "FOOD", Name = "Food", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Code = "ELEC" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("ELEC", result.Items[0].Code);
    }

    [Fact]
    public async Task GetAllAsync_CursorPagination_ReturnsItemsAfterId()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Code = "C", Name = "Charlie", CreatedAt = DateTime.UtcNow }
        );

        var cursor = Helpers.CursorHelper.Encode(1); // cursor after ID 1
        var query = new CategoryQueryParameters { Cursor = cursor, Limit = 10 };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items[0].Id > 1);
        // Total should be 0 when using cursor pagination (COUNT skipped)
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetAllAsync_OffsetPagination_RespectsPageAndLimit()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Code = "C", Name = "Charlie", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Page = 1, Limit = 2 };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task GetAllAsync_HasMore_WhenMoreItemsThanLimit()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Code = "C", Name = "Charlie", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Limit = 2 };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.True(result.HasMore);
        Assert.NotNull(result.Cursor);
    }

    [Fact]
    public async Task GetAllAsync_SortByIdDesc()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "id:desc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal(1, result.Items[1].Id);
    }

    [Fact]
    public async Task GetAllAsync_SortByCode()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "ZZZ", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "AAA", Name = "Bravo", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "code:asc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal("AAA", result.Items[0].Code);
        Assert.Equal("ZZZ", result.Items[1].Code);
    }

    [Fact]
    public async Task GetAllAsync_SortByCreatedAt()
    {
        var older = DateTime.UtcNow.AddDays(-1);
        var newer = DateTime.UtcNow;
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = newer },
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = older }
        );

        var query = new CategoryQueryParameters { Sort = "created_at:asc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal("Bravo", result.Items[0].Name);
        Assert.Equal("Alpha", result.Items[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_MultiColumnSort()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "B", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "name:asc,id:desc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal("Alpha", result.Items[0].Name);
        Assert.Equal("Bravo", result.Items[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_SortByIdAsc()
    {
        await SeedAsync(
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "id:asc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(1, result.Items[0].Id);
        Assert.Equal(2, result.Items[1].Id);
    }

    [Fact]
    public async Task GetAllAsync_SortByNameDesc()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "name:desc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal("Bravo", result.Items[0].Name);
        Assert.Equal("Alpha", result.Items[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_SortByCodeDesc()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "AAA", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "ZZZ", Name = "Bravo", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "code:desc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal("ZZZ", result.Items[0].Code);
        Assert.Equal("AAA", result.Items[1].Code);
    }

    [Fact]
    public async Task GetAllAsync_SortByCreatedAtDesc()
    {
        var older = DateTime.UtcNow.AddDays(-1);
        var newer = DateTime.UtcNow;
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = older },
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = newer }
        );

        var query = new CategoryQueryParameters { Sort = "created_at:desc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal("Bravo", result.Items[0].Name);
        Assert.Equal("Alpha", result.Items[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_MultiColumnSort_ThenByCreatedAt()
    {
        var older = DateTime.UtcNow.AddDays(-1);
        var newer = DateTime.UtcNow;
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = newer },
            new Category { Id = 2, Code = "B", Name = "Alpha", CreatedAt = older }
        );

        var query = new CategoryQueryParameters { Sort = "name:asc,created_at:asc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAllAsync_MultiColumnSort_ThenByCode()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "ZZZ", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "AAA", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "name:asc,code:asc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAllAsync_MultiColumnSort_ThenByName()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "id:asc,name:desc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAllAsync_MultiColumnSort_ThenByInvalidColumn_IgnoredGracefully()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "name:asc,nonexistent:desc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal("Alpha", result.Items[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_CommaSeparatedEmptySort_FallsBackToDefault()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "," };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        // Falls back to default (name ASC) via ordered ?? fallback
        Assert.Equal("Alpha", result.Items[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_SortByColumnWithoutDirection_DefaultsToAsc()
    {
        await SeedAsync(
            new Category { Id = 2, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "id" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(1, result.Items[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_MultiColumnSort_ThenByIdAsc()
    {
        await SeedAsync(
            new Category { Id = 2, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 1, Code = "B", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "name:asc,id:asc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(1, result.Items[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_MultiColumnSort_ThenByCreatedAtDesc()
    {
        var older = DateTime.UtcNow.AddDays(-1);
        var newer = DateTime.UtcNow;
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = older },
            new Category { Id = 2, Code = "B", Name = "Alpha", CreatedAt = newer }
        );

        var query = new CategoryQueryParameters { Sort = "name:asc,created_at:desc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAllAsync_MultiColumnSort_ThenByCodeDesc()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "ZZZ", Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "AAA", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "name:asc,code:desc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal("ZZZ", result.Items[0].Code);
    }

    [Fact]
    public async Task GetAllAsync_MultiColumnSort_ThenByNameAsc()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "code:asc,name:asc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        Assert.Equal("Alpha", result.Items[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_InvalidSortColumn_FallsBackToDefaultSort()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "B", Name = "Bravo", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Sort = "nonexistent:asc" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        // Falls back to default (name ASC)
        Assert.Equal("Alpha", result.Items[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_InvalidCursor_IgnoredGracefully()
    {
        await SeedAsync(
            new Category { Id = 1, Code = "A", Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );

        var query = new CategoryQueryParameters { Cursor = "invalid-base64!!!" };
        var result = await _repository.GetAllAsync(query, CancellationToken.None);

        // Cursor decode returns null, so treated as cursor with no filter
        Assert.Single(result.Items);
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
