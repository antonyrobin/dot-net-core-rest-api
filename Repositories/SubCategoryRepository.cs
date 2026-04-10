using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Helpers;
using dot_net_core_rest_api.Models;
using Npgsql;

namespace dot_net_core_rest_api.Repositories;

public class SubCategoryRepository(NpgsqlDataSource dataSource, ILogger<SubCategoryRepository> logger) : ISubCategoryRepository
{
    private static readonly HashSet<string> AllowedSortColumns = ["id", "created_at", "code", "name", "category_id"];

    public async Task<PagedResult<SubCategory>> GetAllAsync(SubCategoryQueryParameters query, CancellationToken ct)
    {
        logger.LogDebug("Querying sub-categories: Page={Page}, Limit={Limit}, Cursor={Cursor}, Sort={Sort}, Name={Name}, Code={Code}, CategoryId={CategoryId}",
            query.Page, query.Limit, query.Cursor, query.Sort, query.Name, query.Code, query.CategoryId);

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var limit = query.Limit ?? 20;

        // Build filter conditions
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Name)) filters.Add("name ILIKE @name");
        if (!string.IsNullOrWhiteSpace(query.Code)) filters.Add("code ILIKE @code");
        if (query.CategoryId.HasValue) filters.Add("category_id = @categoryId");

        var filterWhere = filters.Count > 0 ? "WHERE " + string.Join(" AND ", filters) : "";

        // Skip COUNT for cursor pagination (total is not needed)
        var total = 0;
        if (string.IsNullOrWhiteSpace(query.Cursor))
        {
            await using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM sub_categories {filterWhere}", conn);
            AddFilterParams(countCmd, query);
            total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
            logger.LogDebug("Total sub-categories matching filter: {Total}", total);
        }

        // Cursor handling – add cursor condition after count
        int? cursorId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            cursorId = CursorHelper.Decode(query.Cursor);
            if (cursorId.HasValue)
            {
                filters.Add("id > @cursorId");
                logger.LogDebug("Applying cursor-based pagination from ID {CursorId}", cursorId.Value);
            }
        }

        var dataWhere = filters.Count > 0 ? "WHERE " + string.Join(" AND ", filters) : "";

        // Sorting & offset
        string orderBy;
        int offset = 0;
        if (cursorId.HasValue)
        {
            orderBy = "ORDER BY id ASC";
        }
        else
        {
            orderBy = "ORDER BY " + SortHelper.BuildOrderByClause(query.Sort, AllowedSortColumns);
            offset = ((query.Page ?? 1) - 1) * limit;
        }

        var sql = $"SELECT id, created_at, code, name, category_id FROM sub_categories {dataWhere} {orderBy} LIMIT @limit OFFSET @offset";

        await using var cmd = new NpgsqlCommand(sql, conn);
        AddFilterParams(cmd, query);
        if (cursorId.HasValue)
            cmd.Parameters.AddWithValue("cursorId", cursorId.Value);
        cmd.Parameters.AddWithValue("limit", limit + 1);
        cmd.Parameters.AddWithValue("offset", offset);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var items = new List<SubCategory>();
        while (await reader.ReadAsync(ct))
            items.Add(MapRow(reader));

        var hasMore = items.Count > limit;
        if (hasMore)
            items = items.Take(limit).ToList();

        var cursor = items.Count > 0 ? CursorHelper.Encode(items[^1].Id) : null;

        logger.LogDebug("Returning {Count} sub-categories, HasMore={HasMore}", items.Count, hasMore);

        return new PagedResult<SubCategory>
        {
            Items = items,
            Total = total,
            Cursor = cursor,
            HasMore = hasMore
        };
    }

    public async Task<SubCategory?> GetByIdAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Getting sub-category by ID: {SubCategoryId}", id);

        const string sql = """
            SELECT id, created_at, code, name, category_id
            FROM sub_categories
            WHERE id = @id
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        return await reader.ReadAsync(ct) ? MapRow(reader) : null;
    }

    public async Task<SubCategory> CreateAsync(SubCategory subCategory, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO sub_categories (code, name, category_id, created_at)
            VALUES (@code, @name, @categoryId, @createdAt)
            RETURNING id, created_at
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("code", subCategory.Code);
        cmd.Parameters.AddWithValue("name", subCategory.Name);
        cmd.Parameters.AddWithValue("categoryId", subCategory.CategoryId);
        cmd.Parameters.AddWithValue("createdAt", subCategory.CreatedAt);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            subCategory.Id = reader.GetInt32(0);
            subCategory.CreatedAt = reader.GetDateTime(1);
        }

        logger.LogDebug("Inserted sub-category with ID: {SubCategoryId}", subCategory.Id);
        return subCategory;
    }

    public async Task UpdateAsync(SubCategory subCategory, CancellationToken ct)
    {
        const string sql = """
            UPDATE sub_categories
            SET code = @code, name = @name, category_id = @categoryId
            WHERE id = @id
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", subCategory.Id);
        cmd.Parameters.AddWithValue("code", subCategory.Code);
        cmd.Parameters.AddWithValue("name", subCategory.Name);
        cmd.Parameters.AddWithValue("categoryId", subCategory.CategoryId);
        await cmd.ExecuteNonQueryAsync(ct);

        logger.LogDebug("Updated sub-category ID: {SubCategoryId}", subCategory.Id);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        const string sql = """
            DELETE FROM sub_categories
            WHERE id = @id
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);

        if (rowsAffected > 0)
            logger.LogDebug("Deleted sub-category ID: {SubCategoryId}", id);
        else
            logger.LogDebug("Sub-category not found for deletion: {SubCategoryId}", id);

        return rowsAffected > 0;
    }

    private static void AddFilterParams(NpgsqlCommand cmd, SubCategoryQueryParameters query)
    {
        if (!string.IsNullOrWhiteSpace(query.Name))
            cmd.Parameters.AddWithValue("name", $"%{query.Name}%");
        if (!string.IsNullOrWhiteSpace(query.Code))
            cmd.Parameters.AddWithValue("code", $"%{query.Code}%");
        if (query.CategoryId.HasValue)
            cmd.Parameters.AddWithValue("categoryId", query.CategoryId.Value);
    }

    private static SubCategory MapRow(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("id")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        Code = reader.GetString(reader.GetOrdinal("code")),
        Name = reader.GetString(reader.GetOrdinal("name")),
        CategoryId = reader.GetInt32(reader.GetOrdinal("category_id"))
    };
}
