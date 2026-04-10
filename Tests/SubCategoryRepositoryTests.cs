using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Models;
using dot_net_core_rest_api.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;

namespace dot_net_core_rest_api.Tests;

public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        DataSource = NpgsqlDataSource.Create(_container.GetConnectionString());

        await using var conn = await DataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("""
            CREATE TABLE categories (
                id          SERIAL PRIMARY KEY,
                created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
                code        VARCHAR     NOT NULL UNIQUE,
                name        VARCHAR     NOT NULL UNIQUE
            );
            CREATE TABLE sub_categories (
                id          SERIAL PRIMARY KEY,
                created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
                code        VARCHAR     NOT NULL UNIQUE,
                name        VARCHAR     NOT NULL UNIQUE,
                category_id INT         NOT NULL REFERENCES categories(id) ON DELETE CASCADE
            );
            INSERT INTO categories (code, name) VALUES ('CAT1', 'Category 1');
            INSERT INTO categories (code, name) VALUES ('CAT2', 'Category 2');
            """, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}

public class SubCategoryRepositoryTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private readonly SubCategoryRepository _repository;

    public SubCategoryRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _repository = new SubCategoryRepository(fixture.DataSource, new Mock<ILogger<SubCategoryRepository>>().Object);
    }

    public async Task InitializeAsync()
    {
        await using var conn = await _fixture.DataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("TRUNCATE sub_categories RESTART IDENTITY CASCADE", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedAsync(params SubCategory[] items)
    {
        foreach (var sc in items)
        {
            await using var conn = await _fixture.DataSource.OpenConnectionAsync();
            await using var cmd = new NpgsqlCommand("""
                INSERT INTO sub_categories (code, name, category_id, created_at)
                VALUES (@code, @name, @categoryId, @createdAt)
                """, conn);
            cmd.Parameters.AddWithValue("code", sc.Code);
            cmd.Parameters.AddWithValue("name", sc.Name);
            cmd.Parameters.AddWithValue("categoryId", sc.CategoryId);
            cmd.Parameters.AddWithValue("createdAt", sc.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // ───── GetAllAsync ─────

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrderedByName()
    {
        await SeedAsync(
            new SubCategory { Code = "B", Name = "Bravo", CategoryId = 1, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "A", Name = "Alpha", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var result = await _repository.GetAllAsync(new SubCategoryQueryParameters(), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Alpha", result.Items[0].Name);
        Assert.Equal("Bravo", result.Items[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_EmptyTable_ReturnsEmptyList()
    {
        var result = await _repository.GetAllAsync(new SubCategoryQueryParameters(), CancellationToken.None);

        Assert.Empty(result.Items);
    }

    // ───── GetAllAsync with CategoryId filter ─────

    [Fact]
    public async Task GetAllAsync_WithCategoryId_ReturnsMatchingRows()
    {
        await SeedAsync(
            new SubCategory { Code = "S1", Name = "Sub1", CategoryId = 1, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "S2", Name = "Sub2", CategoryId = 2, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "S3", Name = "Sub3", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var result = await _repository.GetAllAsync(new SubCategoryQueryParameters { CategoryId = 1 }, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, r => Assert.Equal(1, r.CategoryId));
    }

    [Fact]
    public async Task GetAllAsync_WithCategoryId_NoMatch_ReturnsEmptyList()
    {
        await SeedAsync(
            new SubCategory { Code = "S1", Name = "Sub1", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var result = await _repository.GetAllAsync(new SubCategoryQueryParameters { CategoryId = 999 }, CancellationToken.None);

        Assert.Empty(result.Items);
    }

    // ───── GetByIdAsync ─────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsSubCategory()
    {
        await SeedAsync(
            new SubCategory { Code = "SC1", Name = "Test", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var all = await _repository.GetAllAsync(new SubCategoryQueryParameters(), CancellationToken.None);
        var result = await _repository.GetByIdAsync(all.Items[0].Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("SC1", result!.Code);
        Assert.Equal("Test", result.Name);
        Assert.Equal(1, result.CategoryId);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(9999, CancellationToken.None);

        Assert.Null(result);
    }

    // ───── CreateAsync ─────

    [Fact]
    public async Task CreateAsync_InsertsAndReturnsWithId()
    {
        var subCategory = new SubCategory
        {
            Code = "NEW",
            Name = "New Sub",
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _repository.CreateAsync(subCategory, CancellationToken.None);

        Assert.True(result.Id > 0);
        Assert.Equal("NEW", result.Code);
        Assert.Equal("New Sub", result.Name);
        Assert.Equal(1, result.CategoryId);

        // Verify it was persisted
        var fromDb = await _repository.GetByIdAsync(result.Id, CancellationToken.None);
        Assert.NotNull(fromDb);
        Assert.Equal("NEW", fromDb!.Code);
    }

    // ───── UpdateAsync ─────

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await SeedAsync(
            new SubCategory { Code = "OLD", Name = "Old Name", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var all = await _repository.GetAllAsync(new SubCategoryQueryParameters(), CancellationToken.None);
        var existing = all.Items[0];

        var updated = new SubCategory
        {
            Id = existing.Id,
            Code = "UPD",
            Name = "Updated",
            CategoryId = 2,
            CreatedAt = existing.CreatedAt
        };

        await _repository.UpdateAsync(updated, CancellationToken.None);

        var fromDb = await _repository.GetByIdAsync(existing.Id, CancellationToken.None);
        Assert.NotNull(fromDb);
        Assert.Equal("UPD", fromDb!.Code);
        Assert.Equal("Updated", fromDb.Name);
        Assert.Equal(2, fromDb.CategoryId);
    }

    // ───── DeleteAsync ─────

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesAndReturnsTrue()
    {
        await SeedAsync(
            new SubCategory { Code = "DEL", Name = "Delete Me", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var all = await _repository.GetAllAsync(new SubCategoryQueryParameters(), CancellationToken.None);
        var id = all.Items[0].Id;

        var result = await _repository.DeleteAsync(id, CancellationToken.None);

        Assert.True(result);
        Assert.Null(await _repository.GetByIdAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(9999, CancellationToken.None);

        Assert.False(result);
    }

    // ───── GetAllAsync with Name filter ─────

    [Fact]
    public async Task GetAllAsync_WithNameFilter_ReturnsMatchingRows()
    {
        await SeedAsync(
            new SubCategory { Code = "S1", Name = "Alpha Sub", CategoryId = 1, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "S2", Name = "Bravo Sub", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var result = await _repository.GetAllAsync(new SubCategoryQueryParameters { Name = "Alpha" }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Alpha Sub", result.Items[0].Name);
    }

    // ───── GetAllAsync with Code filter ─────

    [Fact]
    public async Task GetAllAsync_WithCodeFilter_ReturnsMatchingRows()
    {
        await SeedAsync(
            new SubCategory { Code = "XYZ", Name = "Sub1", CategoryId = 1, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "ABC", Name = "Sub2", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var result = await _repository.GetAllAsync(new SubCategoryQueryParameters { Code = "XYZ" }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("XYZ", result.Items[0].Code);
    }

    // ───── GetAllAsync with combined filters ─────

    [Fact]
    public async Task GetAllAsync_WithAllFilters_ReturnsMatchingRows()
    {
        await SeedAsync(
            new SubCategory { Code = "F1", Name = "Filter1", CategoryId = 1, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "F2", Name = "Filter2", CategoryId = 2, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "X1", Name = "Other", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var result = await _repository.GetAllAsync(
            new SubCategoryQueryParameters { Name = "Filter", Code = "F1", CategoryId = 1 },
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("F1", result.Items[0].Code);
    }

    // ───── GetAllAsync with cursor pagination ─────

    [Fact]
    public async Task GetAllAsync_WithCursor_ReturnsPaginatedResults()
    {
        await SeedAsync(
            new SubCategory { Code = "C1", Name = "Sub1", CategoryId = 1, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "C2", Name = "Sub2", CategoryId = 1, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "C3", Name = "Sub3", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        // First page
        var first = await _repository.GetAllAsync(new SubCategoryQueryParameters { Limit = 2 }, CancellationToken.None);
        Assert.Equal(2, first.Items.Count);
        Assert.True(first.HasMore);
        Assert.NotNull(first.Cursor);

        // Second page using cursor
        var second = await _repository.GetAllAsync(new SubCategoryQueryParameters { Cursor = first.Cursor, Limit = 2 }, CancellationToken.None);
        Assert.Single(second.Items);
        Assert.False(second.HasMore);
    }

    // ───── GetAllAsync with sorting ─────

    [Fact]
    public async Task GetAllAsync_WithSort_ReturnsSortedResults()
    {
        await SeedAsync(
            new SubCategory { Code = "B", Name = "Bravo", CategoryId = 1, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "A", Name = "Alpha", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var result = await _repository.GetAllAsync(new SubCategoryQueryParameters { Sort = "code:asc" }, CancellationToken.None);

        Assert.Equal("A", result.Items[0].Code);
        Assert.Equal("B", result.Items[1].Code);
    }

    // ───── GetAllAsync with offset pagination ─────

    [Fact]
    public async Task GetAllAsync_WithOffsetPagination_ReturnsCorrectPage()
    {
        await SeedAsync(
            new SubCategory { Code = "P1", Name = "Page1", CategoryId = 1, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "P2", Name = "Page2", CategoryId = 1, CreatedAt = DateTime.UtcNow },
            new SubCategory { Code = "P3", Name = "Page3", CategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        var result = await _repository.GetAllAsync(new SubCategoryQueryParameters { Page = 2, Limit = 2 }, CancellationToken.None);

        Assert.Single(result.Items);
    }
}
