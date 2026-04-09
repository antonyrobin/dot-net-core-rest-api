using System.ComponentModel.DataAnnotations;

namespace dot_net_core_rest_api.Dtos;

public record SubCategoryDto(
    int Id,
    DateTime CreatedAt,
    string Code,
    string Name,
    int CategoryId
);

public record CreateSubCategoryRequest(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Range(1, int.MaxValue)] int CategoryId
);

public record UpdateSubCategoryRequest(
    [StringLength(50)] string? Code,
    [StringLength(200)] string? Name,
    [Range(1, int.MaxValue)] int? CategoryId
);
