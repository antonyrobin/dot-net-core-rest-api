using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Repositories;

namespace dot_net_core_rest_api.Services;

public class SubCategoryService(ISubCategoryRepository repository, ILogger<SubCategoryService> logger) : ISubCategoryService
{
    public async Task<List<SubCategoryDto>> GetAllAsync(CancellationToken ct)
    {
        var subCategories = await repository.GetAllAsync(ct);
        return subCategories.Select(ToDto).ToList();
    }

    public async Task<List<SubCategoryDto>> GetByCategoryIdAsync(int categoryId, CancellationToken ct)
    {
        var subCategories = await repository.GetByCategoryIdAsync(categoryId, ct);
        return subCategories.Select(ToDto).ToList();
    }

    public async Task<SubCategoryDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var subCategory = await repository.GetByIdAsync(id, ct);
        return subCategory is null ? null : ToDto(subCategory);
    }

    public async Task<SubCategoryDto> CreateAsync(CreateSubCategoryRequest request, CancellationToken ct)
    {
        var subCategory = new SubCategory
        {
            Code = request.Code,
            Name = request.Name,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(subCategory, ct);

        logger.LogInformation("SubCategory created: {SubCategoryId} {SubCategoryCode}", subCategory.Id, subCategory.Code);

        return ToDto(subCategory);
    }

    public async Task<SubCategoryDto?> UpdateAsync(int id, UpdateSubCategoryRequest request, CancellationToken ct)
    {
        var subCategory = await repository.GetByIdAsync(id, ct);
        if (subCategory is null)
            return null;

        if (request.Code is not null)
            subCategory.Code = request.Code;

        if (request.Name is not null)
            subCategory.Name = request.Name;

        if (request.CategoryId is not null)
            subCategory.CategoryId = request.CategoryId.Value;

        await repository.UpdateAsync(subCategory, ct);

        logger.LogInformation("SubCategory updated: {SubCategoryId}", subCategory.Id);

        return ToDto(subCategory);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);

        if (deleted)
            logger.LogInformation("SubCategory deleted: {SubCategoryId}", id);

        return deleted;
    }

    private static SubCategoryDto ToDto(SubCategory s) => new(s.Id, s.CreatedAt, s.Code, s.Name, s.CategoryId);
}
