# .NET Core REST API with PostgreSQL (Supabase)

A clean-architecture REST API built with **ASP.NET Core (.NET 10)**, **Entity Framework Core**, and **PostgreSQL** (Supabase cloud). Follows the **Repository–Service–Controller** pattern with full unit test coverage.

## Table of Contents

- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [1. Create the Project](#1-create-the-project)
- [2. Install NuGet Packages](#2-install-nuget-packages)
- [3. Create Entities](#3-create-entities)
- [4. Create DTOs](#4-create-dtos)
- [5. Configure the Database](#5-configure-the-database)
  - [5.1 Connection String (appsettings.json)](#51-connection-string-appsettingsjson)
  - [5.2 DbContext](#52-dbcontext)
  - [5.3 Table Configuration (Fluent API)](#53-table-configuration-fluent-api)
  - [5.4 Register DbContext in Program.cs](#54-register-dbcontext-in-programcs)
- [6. Create Repositories](#6-create-repositories)
- [7. Create Services](#7-create-services)
- [8. Create Controllers](#8-create-controllers)
- [9. Register DI in Program.cs](#9-register-di-in-programcs)
- [10. Run the Application](#10-run-the-application)
- [11. API Endpoints](#11-api-endpoints)
- [12. Unit Testing](#12-unit-testing)
  - [12.1 Create the Test Project](#121-create-the-test-project)
  - [12.2 Install Test Packages](#122-install-test-packages)
  - [12.3 Test Structure](#123-test-structure)
  - [12.4 Run Tests](#124-run-tests)
  - [12.5 Code Coverage Report](#125-code-coverage-report)
- [13. Docker](#13-docker)
- [14. Design Patterns Used](#14-design-patterns-used)
- [15. Supabase Connection Notes](#15-supabase-connection-notes)

---

## Project Structure

```
dot-net-core-rest-api/
├── Controllers/
│   └── CategoriesController.cs        # API endpoints (HTTP layer)
├── Data/
│   ├── AppDbContext.cs                 # EF Core DbContext
│   └── Configurations/
│       └── CategoryConfiguration.cs   # Fluent API table mapping
├── Dtos/
│   └── CategoryDtos.cs                # Request/Response records
├── Entities/
│   └── Category.cs                    # Domain model
├── Repositories/
│   ├── ICategoryRepository.cs         # Repository interface
│   └── CategoryRepository.cs          # DB operations
├── Services/
│   ├── ICategoryService.cs            # Service interface
│   └── CategoryService.cs             # Business logic + DTO mapping
├── Tests/
│   ├── CategoriesControllerTests.cs   # Controller unit tests
│   ├── CategoryServiceTests.cs        # Service unit tests
│   └── CategoryRepositoryTests.cs     # Repository unit tests
├── Properties/
│   └── launchSettings.json
├── Program.cs                         # App entry point & DI config
├── appsettings.json                   # Configuration
├── Dockerfile
├── .gitignore
└── dot-net-core-rest-api.csproj
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/) or a [Supabase](https://supabase.com/) account
- (Optional) [Docker](https://www.docker.com/)

---

## 1. Create the Project

```bash
dotnet new webapi -n dot-net-core-rest-api -controllers
cd dot-net-core-rest-api
```

---

## 2. Install NuGet Packages

### Main Project

```bash
# EF Core PostgreSQL provider
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# OpenAPI support (included by default in webapi template)
dotnet add package Microsoft.AspNetCore.OpenApi
```

### Test Project (see [Unit Testing](#12-unit-testing) section)

```bash
dotnet add package Moq
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package coverlet.msbuild
```

---

## 3. Create Entities

Entities represent your database tables as C# classes. Place them in the `Entities/` folder.

**`Entities/Category.cs`**

```csharp
namespace dot_net_core_rest_api.Entities;

public class Category
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
```

---

## 4. Create DTOs

DTOs (Data Transfer Objects) separate your API contracts from internal entities. Use C# `record` types for immutability.

**`Dtos/CategoryDtos.cs`**

```csharp
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
```

---

## 5. Configure the Database

### 5.1 Connection String (appsettings.json)

Add your PostgreSQL connection string. For **Supabase connection pooler** (PgBouncer), include `No Reset On Close` and `Multiplexing=false`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.YOUR_PROJECT_REF;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;No Reset On Close=true;Multiplexing=false"
  }
}
```

> **Important:** `No Reset On Close=true` and `Multiplexing=false` are required for Supabase's PgBouncer pooler. Without these, you'll get `ObjectDisposedException` errors on write operations.

> **Security Tip:** Don't commit passwords. Use `dotnet user-secrets` for local development:
> ```bash
> dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Password=YOUR_PASSWORD;..."
> ```

### 5.2 DbContext

The `AppDbContext` is your EF Core session with the database. It exposes `DbSet<T>` properties for each table.

**`Data/AppDbContext.cs`**

```csharp
using dot_net_core_rest_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_rest_api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

### 5.3 Table Configuration (Fluent API)

Map C# entity properties to PostgreSQL column names and types. PostgreSQL is **case-sensitive** — if your Supabase table uses lowercase names, configure them explicitly.

**`Data/Configurations/CategoryConfiguration.cs`**

```csharp
using dot_net_core_rest_api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dot_net_core_rest_api.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");          // exact table name in PostgreSQL

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasColumnType("varchar")
            .IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
```

> **Note:** The table name in `ToTable("categories")` must exactly match the name in your database. Run this SQL in Supabase to verify:
> ```sql
> SELECT table_name FROM information_schema.tables
> WHERE table_schema = 'public' AND table_name ILIKE 'categor%';
> ```

### 5.4 Register DbContext in Program.cs

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

`AddDbContext` registers `AppDbContext` with **Scoped** lifetime (one instance per HTTP request) — this is the correct lifetime for DbContext.

---

## 6. Create Repositories

The **Repository layer** handles all database operations. It works with **Entity** objects only (no DTOs).

**`Repositories/ICategoryRepository.cs`**

```csharp
using dot_net_core_rest_api.Entities;

namespace dot_net_core_rest_api.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(CancellationToken ct);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct);
    Task<Category> CreateAsync(Category category, CancellationToken ct);
    Task UpdateAsync(Category category, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
```

**`Repositories/CategoryRepository.cs`**

```csharp
using dot_net_core_rest_api.Data;
using dot_net_core_rest_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_rest_api.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<List<Category>> GetAllAsync(CancellationToken ct)
    {
        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Category> CreateAsync(Category category, CancellationToken ct)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken ct)
    {
        db.Categories.Update(category);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null)
            return false;

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
```

---

## 7. Create Services

The **Service layer** contains business logic and maps between entities and DTOs. It depends on the repository interface, not the DbContext directly.

**`Services/ICategoryService.cs`**

```csharp
using dot_net_core_rest_api.Dtos;

namespace dot_net_core_rest_api.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(CancellationToken ct);
    Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
```

**`Services/CategoryService.cs`**

```csharp
using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Entities;
using dot_net_core_rest_api.Repositories;

namespace dot_net_core_rest_api.Services;

public class CategoryService(ICategoryRepository repository, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken ct)
    {
        var categories = await repository.GetAllAsync(ct);
        return categories.Select(ToDto).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(id, ct);
        return category is null ? null : ToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = new Category
        {
            Code = request.Code,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(category, ct);
        logger.LogInformation("Category created: {CategoryId} {CategoryCode}", category.Id, category.Code);
        return ToDto(category);
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(id, ct);
        if (category is null)
            return null;

        if (request.Code is not null) category.Code = request.Code;
        if (request.Name is not null) category.Name = request.Name;

        await repository.UpdateAsync(category, ct);
        logger.LogInformation("Category updated: {CategoryId}", category.Id);
        return ToDto(category);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        if (deleted) logger.LogInformation("Category deleted: {CategoryId}", id);
        return deleted;
    }

    private static CategoryDto ToDto(Category c) => new(c.Id, c.CreatedAt, c.Code, c.Name);
}
```

---

## 8. Create Controllers

Controllers handle HTTP requests, delegate to the service layer, and return appropriate HTTP responses.

**`Controllers/CategoriesController.cs`**

```csharp
using dot_net_core_rest_api.Dtos;
using dot_net_core_rest_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dot_net_core_rest_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var categories = await categoryService.GetAllAsync(ct);
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var category = await categoryService.GetByIdAsync(id, ct);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.UpdateAsync(id, request, ct);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await categoryService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
```

---

## 9. Register DI in Program.cs

All dependencies are registered in `Program.cs` using **Scoped** lifetime (one instance per HTTP request):

**`Program.cs`**

```csharp
using dot_net_core_rest_api.Data;
using dot_net_core_rest_api.Repositories;
using dot_net_core_rest_api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Services (DI) ----------
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// ---------- Controllers & OpenAPI ----------
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

**Adding a new feature (e.g., Products)?** Follow the same pattern:
1. Create `Entities/Product.cs`
2. Create `Dtos/ProductDtos.cs`
3. Create `Data/Configurations/ProductConfiguration.cs`
4. Create `Repositories/IProductRepository.cs` + `ProductRepository.cs`
5. Create `Services/IProductService.cs` + `ProductService.cs`
6. Create `Controllers/ProductsController.cs`
7. Add `DbSet<Product>` to `AppDbContext`
8. Register in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<IProductRepository, ProductRepository>();
   builder.Services.AddScoped<IProductService, ProductService>();
   ```

---

## 10. Run the Application

```bash
# Run in development mode
dotnet run

# Run with hot reload
dotnet watch run
```

The API will be available at `http://localhost:5101`.

---

## 11. API Endpoints

| Method   | URL                      | Description           | Request Body                            |
|----------|--------------------------|-----------------------|-----------------------------------------|
| `GET`    | `/api/categories`        | Get all categories    | —                                       |
| `GET`    | `/api/categories/{id}`   | Get category by ID    | —                                       |
| `POST`   | `/api/categories`        | Create a category     | `{ "code": "ELEC", "name": "Electronics" }` |
| `PUT`    | `/api/categories/{id}`   | Update a category     | `{ "code": "UPD", "name": "Updated" }`  |
| `DELETE` | `/api/categories/{id}`   | Delete a category     | —                                       |

### Test with curl

```bash
# Get all
curl http://localhost:5101/api/categories

# Create
curl -X POST http://localhost:5101/api/categories \
  -H "Content-Type: application/json" \
  -d '{"code": "ELEC", "name": "Electronics"}'

# Update
curl -X PUT http://localhost:5101/api/categories/1 \
  -H "Content-Type: application/json" \
  -d '{"name": "Updated Electronics"}'

# Delete
curl -X DELETE http://localhost:5101/api/categories/1
```

You can also use the `.http` file (`dot-net-core-rest-api.http`) directly in VS Code with the REST Client extension.

---

## 12. Unit Testing

### 12.1 Create the Test Project

```bash
dotnet new xunit -n dot-net-core-rest-api.Tests -o Tests --framework net10.0
cd Tests
dotnet add reference ../dot-net-core-rest-api.csproj
```

> **Important:** Since the `Tests/` folder is inside the main project directory, add this to the main `.csproj` to prevent the main project from compiling test files:
> ```xml
> <DefaultItemExcludes>$(DefaultItemExcludes);Tests\**</DefaultItemExcludes>
> ```

### 12.2 Install Test Packages

```bash
cd Tests

# Mocking framework
dotnet add package Moq

# EF Core in-memory database for repository tests
dotnet add package Microsoft.EntityFrameworkCore.InMemory

# Code coverage (MSBuild integration)
dotnet add package coverlet.msbuild
```

### 12.3 Test Structure

| Test File                         | Layer      | Technique                              |
|-----------------------------------|------------|----------------------------------------|
| `CategoriesControllerTests.cs`    | Controller | Mocks `ICategoryService`               |
| `CategoryServiceTests.cs`         | Service    | Mocks `ICategoryRepository` + `ILogger`|
| `CategoryRepositoryTests.cs`      | Repository | EF Core InMemory database              |

**Testing approach per layer:**

- **Controller tests** — Mock the service interface. Verify correct HTTP status codes (`Ok`, `NotFound`, `CreatedAtAction`, `NoContent`).
- **Service tests** — Mock the repository interface. Verify DTO mapping, partial updates, null handling, and that repository methods are called correctly.
- **Repository tests** — Use `UseInMemoryDatabase` for a real DbContext without a database. Verify CRUD operations, ordering, and return values.

### 12.4 Run Tests

```bash
cd Tests

# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run a specific test class
dotnet test --filter "CategoryServiceTests"

# Run a specific test method
dotnet test --filter "GetById_NonExistingId_ReturnsNotFound"
```

### 12.5 Code Coverage Report

```bash
cd Tests

# 1. Run tests with coverage collection
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/ /p:Include="[dot-net-core-rest-api]*"

# 2. Install the report generator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# 3. Generate the HTML report
reportgenerator -reports:./coverage/coverage.cobertura.xml -targetdir:./coverage/report -reporttypes:Html

# 4. Open in browser
# Windows
start ./coverage/report/index.html
# macOS
open ./coverage/report/index.html
# Linux
xdg-open ./coverage/report/index.html
```

The HTML report shows per-file, per-method, and line-by-line coverage with green/red highlighting.

---

## 13. Docker

### Build and run

```bash
# Build the image
docker build -t dot-net-core-rest-api .

# Run the container
docker run -d -p 8080:8080 -e "ConnectionStrings__DefaultConnection=Host=...;Port=6543;..." dot-net-core-rest-api
```

### Dockerfile

The project uses a multi-stage Dockerfile:

1. **base** — ASP.NET runtime only (lightweight)
2. **build** — .NET SDK for compiling
3. **publish** — Produces production-ready output
4. **final** — Copies published output into the runtime image

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["dot-net-core-rest-api.csproj", "."]
RUN dotnet restore "./dot-net-core-rest-api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./dot-net-core-rest-api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./dot-net-core-rest-api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dot-net-core-rest-api.dll"]
```

---

## 14. Design Patterns Used

| Pattern                     | Where                                                        |
|-----------------------------|--------------------------------------------------------------|
| **Repository Pattern**      | `ICategoryRepository` / `CategoryRepository`                 |
| **Service Layer Pattern**   | `ICategoryService` / `CategoryService`                       |
| **Dependency Injection**    | Constructor injection throughout all layers                  |
| **DTO Pattern**             | `CategoryDto`, `CreateCategoryRequest`, `UpdateCategoryRequest` |
| **Interface Segregation**   | All layers depend on interfaces, not implementations         |
| **Fluent API Configuration**| `CategoryConfiguration` for EF Core table mapping            |

---

## 15. Supabase Connection Notes

| Issue | Solution |
|---|---|
| `XX000: Tenant or user not found` | Verify the **Host** matches exactly (e.g., `aws-1` vs `aws-0`). Copy from Supabase Dashboard → Settings → Database → Connection Pooling. |
| `42P01: relation "x" does not exist` | PostgreSQL is case-sensitive. Ensure `ToTable("name")` in your EF config matches the exact table name. |
| `ObjectDisposedException: ManualResetEventSlim` | Add `No Reset On Close=true;Multiplexing=false` to the connection string (required for PgBouncer). |
| Verify table name | Run: `SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';` |
| Test connection | Run: `psql "host=... port=6543 dbname=postgres user=postgres.xxx sslmode=require"` |

---

## License

This project is for learning purposes.
