namespace dot_net_core_rest_api.Dtos;

public record CategoryDto(
    int Id,
    DateTime CreatedAt,
    string Code,
    string Name
);

public record CreateCategoryRequest(
    string Code,
    string Name
);

public record UpdateCategoryRequest(
    string? Code,
    string? Name
);
