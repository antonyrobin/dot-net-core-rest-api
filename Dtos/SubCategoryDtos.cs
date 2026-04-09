namespace dot_net_core_rest_api.Dtos;

public record SubCategoryDto(
    int Id,
    DateTime CreatedAt,
    string Code,
    string Name,
    int CategoryId
);

public record CreateSubCategoryRequest(
    string Code,
    string Name,
    int CategoryId
);

public record UpdateSubCategoryRequest(
    string? Code,
    string? Name,
    int? CategoryId
);
