using dot_net_core_rest_api.Data;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Helpers;
using dot_net_core_rest_api.Models;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_rest_api.Repositories;

public class CategoryRepository(AppDbContext db, ILogger<CategoryRepository> logger) : ICategoryRepository
{
    public async Task<PagedResult<Category>> GetAllAsync(CategoryQueryParameters query, CancellationToken ct)
    {
        logger.LogDebug("Querying categories: Page={Page}, Limit={Limit}, Cursor={Cursor}, Sort={Sort}, Name={Name}, Code={Code}",
            query.Page, query.Limit, query.Cursor, query.Sort, query.Name, query.Code);

        var q = db.Categories.AsNoTracking().AsQueryable();

        // Filtering
        if (!string.IsNullOrWhiteSpace(query.Name))
            q = q.Where(c => c.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            q = q.Where(c => c.Code.Contains(query.Code));

        var limit = query.Limit ?? 20;

        // Skip COUNT for cursor pagination (total is not needed)
        var total = string.IsNullOrWhiteSpace(query.Cursor)
            ? await q.CountAsync(ct)
            : 0;
        if (total > 0)
            logger.LogDebug("Total categories matching filter: {Total}", total);

        // Cursor-based pagination
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursorId = CursorHelper.Decode(query.Cursor);
            if (cursorId.HasValue)
            {
                q = q.Where(c => c.Id > cursorId.Value);
                logger.LogDebug("Applying cursor-based pagination from ID {CursorId}", cursorId.Value);
            }
            q = q.OrderBy(c => c.Id);
        }
        else
        {
            // Offset-based pagination with custom sorting
            q = ApplySorting(q, query.Sort);
            var skip = ((query.Page ?? 1) - 1) * limit;
            q = q.Skip(skip);
        }

        var items = await q.Take(limit + 1).ToListAsync(ct);
        var hasMore = items.Count > limit;
        if (hasMore)
            items = items.Take(limit).ToList();

        var cursor = items.Count > 0 ? CursorHelper.Encode(items[^1].Id) : null;

        logger.LogDebug("Returning {Count} categories, HasMore={HasMore}", items.Count, hasMore);

        return new PagedResult<Category>
        {
            Items = items,
            Total = total,
            Cursor = cursor,
            HasMore = hasMore
        };
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct)
    {
        logger.LogDebug("Getting category by ID: {CategoryId}", id);
        return await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Category> CreateAsync(Category category, CancellationToken ct)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        logger.LogDebug("Inserted category with ID: {CategoryId}", category.Id);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken ct)
    {
        db.Categories.Update(category);
        await db.SaveChangesAsync(ct);
        logger.LogDebug("Updated category ID: {CategoryId}", category.Id);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null)
        {
            logger.LogDebug("Category not found for deletion: {CategoryId}", id);
            return false;
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        logger.LogDebug("Deleted category ID: {CategoryId}", id);
        return true;
    }

    private static IQueryable<Category> ApplySorting(IQueryable<Category> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderBy(c => c.Name);

        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Category>? ordered = null;

        foreach (var part in parts)
        {
            var tokens = part.Trim().Split(':');
            var column = tokens[0].Trim().ToLowerInvariant();
            var desc = tokens.Length > 1 && tokens[1].Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);

            if (ordered is null)
            {
                ordered = column switch
                {
                    "id" => desc ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id),
                    "created_at" => desc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
                    "code" => desc ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
                    "name" => desc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                    _ => query.OrderBy(c => c.Name)
                };
            }
            else
            {
                ordered = column switch
                {
                    "id" => desc ? ordered.ThenByDescending(c => c.Id) : ordered.ThenBy(c => c.Id),
                    "created_at" => desc ? ordered.ThenByDescending(c => c.CreatedAt) : ordered.ThenBy(c => c.CreatedAt),
                    "code" => desc ? ordered.ThenByDescending(c => c.Code) : ordered.ThenBy(c => c.Code),
                    "name" => desc ? ordered.ThenByDescending(c => c.Name) : ordered.ThenBy(c => c.Name),
                    _ => ordered
                };
            }
        }

        return ordered ?? query.OrderBy(c => c.Name);
    }
}
