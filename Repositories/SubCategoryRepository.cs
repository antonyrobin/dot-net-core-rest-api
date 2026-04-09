using dot_net_core_rest_api.Entities;
using Npgsql;

namespace dot_net_core_rest_api.Repositories;

public class SubCategoryRepository(NpgsqlDataSource dataSource) : ISubCategoryRepository
{
    public async Task<List<SubCategory>> GetAllAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT id, created_at, code, name, category_id
            FROM sub_categories
            ORDER BY name
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var list = new List<SubCategory>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapRow(reader));
        }
        return list;
    }

    public async Task<List<SubCategory>> GetByCategoryIdAsync(int categoryId, CancellationToken ct)
    {
        const string sql = """
            SELECT id, created_at, code, name, category_id
            FROM sub_categories
            WHERE category_id = @categoryId
            ORDER BY name
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("categoryId", categoryId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var list = new List<SubCategory>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapRow(reader));
        }
        return list;
    }

    public async Task<SubCategory?> GetByIdAsync(int id, CancellationToken ct)
    {
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
        return rowsAffected > 0;
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
