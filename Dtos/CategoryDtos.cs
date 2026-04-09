using System.ComponentModel.DataAnnotations;

namespace dot_net_core_rest_api.Dtos;

public record CategoryDto(
    int Id,
    DateTime CreatedAt,
    string Code,
    string Name
);

public record CreateCategoryRequest(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name
);

public record UpdateCategoryRequest(
    [StringLength(50)] string? Code,
    [StringLength(200)] string? Name
);
