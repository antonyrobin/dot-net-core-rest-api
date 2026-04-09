# .NET Core REST API with PostgreSQL (Supabase)

A clean-architecture REST API built with **ASP.NET Core (.NET 10)**, featuring two data-access strategies — **Entity Framework Core** (Category) and **ADO.NET / Npgsql** (SubCategory) — backed by **PostgreSQL** (Supabase cloud). The project follows the **Repository–Service–Controller** pattern with JWT authentication, rate limiting, security headers, and 100 % test coverage.

---

## Table of Contents

- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [1. Create the Project](#1-create-the-project)
- [2. Install NuGet Packages](#2-install-nuget-packages)
- [3. Create Entities](#3-create-entities)
- [4. Create DTOs (with Validation)](#4-create-dtos-with-validation)
- [5. Configure the Database](#5-configure-the-database)
- [6. Data Access — Entity Framework Core (Category)](#6-data-access--entity-framework-core-category)
- [7. Data Access — ADO.NET / Npgsql (SubCategory)](#7-data-access--adonet--npgsql-subcategory)
- [8. EF Core vs ADO.NET Comparison](#8-ef-core-vs-adonet-comparison)
- [9. Create Services](#9-create-services)
- [10. Create Controllers](#10-create-controllers)
- [11. Security](#11-security)
- [12. Program.cs — DI & Middleware](#12-programcs--di--middleware)
- [13. Environment Variables & Secrets](#13-environment-variables--secrets)
- [14. Run the Application](#14-run-the-application)
- [15. API Endpoints](#15-api-endpoints)
- [16. API Verification — Swagger / OpenAPI & Postman](#16-api-verification--swagger--openapi--postman)
- [17. API Specification for Frontend Team](#17-api-specification-for-frontend-team)
- [18. Unit Testing](#18-unit-testing)
- [19. Integration Testing](#19-integration-testing)
- [20. Code Coverage Report](#20-code-coverage-report)
- [21. Deployment — IIS](#21-deployment--iis)
- [22. Deployment — Docker](#22-deployment--docker)
- [23. Design Patterns Used](#23-design-patterns-used)
- [24. SQL Migrations](#24-sql-migrations)
- [25. Supabase Connection Notes](#25-supabase-connection-notes)

---

## Project Structure

```
dot-net-core-rest-api/
├── Controllers/
│   ├── CategoriesController.cs           # Category endpoints (JWT-protected)
│   └── SubCategoriesController.cs        # SubCategory endpoints (JWT-protected)
├── Data/
│   ├── AppDbContext.cs                    # EF Core DbContext
│   └── Configurations/
│       └── CategoryConfiguration.cs      # Fluent API table mapping
├── Dtos/
│   ├── CategoryDtos.cs                   # Category request/response records (validated)
│   └── SubCategoryDtos.cs                # SubCategory request/response records (validated)
├── Entities/
│   ├── Category.cs                       # Category domain model
│   └── SubCategory.cs                    # SubCategory domain model
├── Migrations/
│   ├── 000_create_categories.sql         # Categories table DDL
│   └── 001_create_sub_categories.sql     # SubCategories table DDL
├── Repositories/
│   ├── ICategoryRepository.cs            # Category repository interface
│   ├── CategoryRepository.cs             # EF Core implementation
│   ├── ISubCategoryRepository.cs         # SubCategory repository interface
│   └── SubCategoryRepository.cs          # ADO.NET / Npgsql implementation
├── Services/
│   ├── ICategoryService.cs               # Category service interface
│   ├── CategoryService.cs                # Business logic + DTO mapping
│   ├── ISubCategoryService.cs            # SubCategory service interface
│   └── SubCategoryService.cs             # Business logic + DTO mapping
├── Tests/
│   ├── CategoriesControllerTests.cs      # Controller unit tests
│   ├── CategoryServiceTests.cs           # Service unit tests
│   ├── CategoryRepositoryTests.cs        # Repository unit tests (InMemory)
│   ├── SubCategoriesControllerTests.cs   # Controller unit tests
│   ├── SubCategoryServiceTests.cs        # Service unit tests
│   ├── SubCategoryRepositoryTests.cs     # Repository integration tests (Testcontainers)
│   ├── DtoTests.cs                       # DTO record tests
│   ├── CategoriesIntegrationTests.cs     # Full-stack integration tests
│   ├── SubCategoriesIntegrationTests.cs  # Full-stack integration tests
│   ├── IntegrationTestFactory.cs         # WebApplicationFactory + Testcontainers
│   └── dot-net-core-rest-api.Tests.csproj
├── Properties/
│   └── launchSettings.json
├── Program.cs                            # Entry point, DI, middleware, security
├── appsettings.json                      # Configuration (JWT, CORS, DB)
├── Dockerfile                            # Multi-stage Docker build
└── dot-net-core-rest-api.csproj
```

---

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | 10.0+ | Build & run the API |
| [PostgreSQL](https://www.postgresql.org/) | 14+ | Database (or use [Supabase](https://supabase.com/)) |
| [Docker Desktop](https://www.docker.com/) | Latest | Container deployment & integration tests |
| [dotnet-reportgenerator](https://github.com/danielpalme/ReportGenerator) | 5.x | Code coverage HTML reports (optional) |

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

# JWT Authentication
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# DTO validation attributes
dotnet add package System.ComponentModel.Annotations

# OpenAPI support (included by default)
dotnet add package Microsoft.AspNetCore.OpenApi
```

### Test Project

```bash
cd Tests
dotnet add package Moq
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers.PostgreSql
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package coverlet.msbuild
dotnet add package coverlet.collector
```

---

## 3. Create Entities

Entities represent database tables. Place them in `Entities/`.

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

**`Entities/SubCategory.cs`**

```csharp
namespace dot_net_core_rest_api.Entities;

public class SubCategory
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}
```

---

## 4. Create DTOs (with Validation)

DTOs use C# `record` types for immutability. Validation attributes enforce constraints at the API boundary before reaching the service layer.

**`Dtos/CategoryDtos.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace dot_net_core_rest_api.Dtos;

public record CategoryDto(int Id, DateTime CreatedAt, string Code, string Name);

public record CreateCategoryRequest(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name
);

public record UpdateCategoryRequest(
    [StringLength(50)] string? Code,
    [StringLength(200)] string? Name
);
```

**`Dtos/SubCategoryDtos.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace dot_net_core_rest_api.Dtos;

public record SubCategoryDto(int Id, DateTime CreatedAt, string Code, string Name, int CategoryId);

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
```

---

## 5. Configure the Database

### 5.1 Connection String (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.YOUR_PROJECT_REF;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;No Reset On Close=true;Multiplexing=false"
  }
}
```

> **Security:** Never commit passwords. Use environment variables or `dotnet user-secrets`:
> ```bash
> dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Password=SECRET;..."
> ```

### 5.2 DbContext

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

```csharp
using dot_net_core_rest_api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dot_net_core_rest_api.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(c => c.Code).HasColumnName("code").HasColumnType("varchar").IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasColumnType("varchar").IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
```

---

## 6. Data Access — Entity Framework Core (Category)

The Category repository uses **Entity Framework Core** with `AppDbContext`. EF Core tracks changes, generates SQL automatically, and manages connection lifetimes.

### Request Flow

```
HTTP Request
  → Controller (validates & delegates)
    → Service (business logic, DTO ↔ Entity mapping)
      → Repository (EF Core DbContext)
        → DbContext.SaveChangesAsync()
          → PostgreSQL
```

### Repository Interface

```csharp
public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(CancellationToken ct);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct);
    Task<Category> CreateAsync(Category category, CancellationToken ct);
    Task UpdateAsync(Category category, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
```

### Repository Implementation

```csharp
public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<List<Category>> GetAllAsync(CancellationToken ct)
        => await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct)
        => await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

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
        if (category is null) return false;
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
```

**Key EF Core features used:**
- `AsNoTracking()` — disables change tracking for read-only queries (better performance)
- `SaveChangesAsync()` — wraps all pending changes in a database transaction automatically
- `ValueGeneratedOnAdd()` — lets PostgreSQL generate `id` and `created_at` values
- Fluent API — maps C# PascalCase properties to PostgreSQL snake_case columns

---

## 7. Data Access — ADO.NET / Npgsql (SubCategory)

The SubCategory repository uses **raw ADO.NET** via `NpgsqlDataSource` and `NpgsqlCommand` with **parameterized queries** to prevent SQL injection. This provides full control over the SQL being executed.

### Request Flow

```
HTTP Request
  → Controller (validates & delegates)
    → Service (business logic, DTO ↔ Entity mapping)
      → Repository (NpgsqlDataSource → NpgsqlCommand)
        → cmd.Parameters.AddWithValue("@param", value)   ← parameterized
        → cmd.ExecuteReaderAsync() / cmd.ExecuteNonQueryAsync()
          → PostgreSQL
```

### How Parameterized Queries Work

Instead of string-concatenating user input into SQL (which is vulnerable to SQL injection), parameterized queries send the SQL template and values separately:

```csharp
// DANGEROUS — SQL injection vulnerability
var sql = $"SELECT * FROM sub_categories WHERE id = {id}";

// SAFE — Parameterized query
const string sql = "SELECT * FROM sub_categories WHERE id = @id";
cmd.Parameters.AddWithValue("id", id);
```

The database engine receives the SQL structure and parameter values independently, making it impossible to inject malicious SQL.

### Transaction / Commit / Rollback Pattern

For operations that need atomicity (multiple statements that must all succeed or all fail), use explicit transactions:

```csharp
await using var conn = await dataSource.OpenConnectionAsync(ct);
await using var transaction = await conn.BeginTransactionAsync(ct);

try
{
    await using var cmd1 = new NpgsqlCommand("INSERT INTO ...", conn, transaction);
    cmd1.Parameters.AddWithValue("param", value);
    await cmd1.ExecuteNonQueryAsync(ct);

    await using var cmd2 = new NpgsqlCommand("UPDATE ...", conn, transaction);
    cmd2.Parameters.AddWithValue("param", value);
    await cmd2.ExecuteNonQueryAsync(ct);

    await transaction.CommitAsync(ct);   // ✅ Both succeed
}
catch
{
    await transaction.RollbackAsync(ct); // ❌ Both rolled back
    throw;
}
```

### Repository Implementation

```csharp
public class SubCategoryRepository(NpgsqlDataSource dataSource) : ISubCategoryRepository
{
    public async Task<SubCategory?> GetByIdAsync(int id, CancellationToken ct)
    {
        const string sql = """
            SELECT id, created_at, code, name, category_id
            FROM sub_categories WHERE id = @id
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

    // ... Update, Delete follow the same pattern

    private static SubCategory MapRow(NpgsqlDataReader reader) => new()
    {
        Id         = reader.GetInt32(0),
        CreatedAt  = reader.GetDateTime(1),
        Code       = reader.GetString(2),
        Name       = reader.GetString(3),
        CategoryId = reader.GetInt32(4)
    };
}
```

**Key ADO.NET features used:**
- `NpgsqlDataSource` — connection pool managed by Npgsql (registered as Singleton)
- `NpgsqlCommand` + `Parameters.AddWithValue()` — parameterized queries preventing SQL injection
- `RETURNING` clause — retrieves server-generated `id` and `created_at` in the same round-trip
- Manual `NpgsqlDataReader` mapping — full control over how rows become objects

---

## 8. EF Core vs ADO.NET Comparison

| Aspect | Entity Framework Core | ADO.NET / Npgsql |
|--------|----------------------|-------------------|
| **Abstraction Level** | High — LINQ queries, change tracking | Low — raw SQL strings |
| **SQL Control** | Generated automatically | Written manually |
| **SQL Injection Protection** | Built-in (parameterized by default) | Manual — must use `Parameters.AddWithValue()` |
| **Transaction Management** | Automatic via `SaveChangesAsync()` | Manual — `BeginTransaction` / `Commit` / `Rollback` |
| **Performance** | Slight overhead (tracking, LINQ translation) | Minimal overhead (direct SQL execution) |
| **Connection Management** | Managed by DbContext (Scoped lifetime) | Manual via `NpgsqlDataSource` (Singleton) |
| **Mapping** | Automatic (Fluent API / conventions) | Manual (`NpgsqlDataReader` → entity) |
| **Migration Support** | Built-in EF migrations | Manual SQL scripts |
| **Learning Curve** | Lower (C# LINQ) | Higher (must know SQL) |
| **Best For** | Standard CRUD, rapid development | Complex queries, performance-critical paths, stored procedures |
| **Used By (this project)** | `CategoryRepository` | `SubCategoryRepository` |

**When to use which:**
- **EF Core** — Default choice for most CRUD operations. Saves development time, prevents common mistakes, and the performance overhead is negligible for typical workloads.
- **ADO.NET** — Use when you need full SQL control: complex joins, CTEs, bulk operations, calling stored procedures, or when microsecond-level performance matters.

---

## 9. Create Services

The Service layer contains business logic, maps between Entities and DTOs, and depends only on repository interfaces.

```csharp
public class CategoryService(ICategoryRepository repository, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = new Category { Code = request.Code, Name = request.Name, CreatedAt = DateTime.UtcNow };
        await repository.CreateAsync(category, ct);
        logger.LogInformation("Category created: {CategoryId} {CategoryCode}", category.Id, category.Code);
        return ToDto(category);
    }

    // GetAll, GetById, Update, Delete follow the same pattern
    private static CategoryDto ToDto(Category c) => new(c.Id, c.CreatedAt, c.Code, c.Name);
}
```

---

## 10. Create Controllers

Controllers handle HTTP requests, apply authorization, and delegate to services.

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var categories = await categoryService.GetAllAsync(ct);
        return Ok(categories);
    }

    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }
    // ...
}
```

**Authorization rules:**
- `[Authorize]` on the class — all endpoints require a valid JWT by default
- `[AllowAnonymous]` on GET endpoints — read operations are public
- POST / PUT / DELETE — require authentication

---

## 11. Security

### 11.1 JWT Bearer Authentication

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
```

Configuration in `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "SET_VIA_ENVIRONMENT_VARIABLE",
    "Issuer": "dot-net-core-rest-api",
    "Audience": "dot-net-core-rest-api-clients"
  }
}
```

### 11.2 Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ReadAccess", policy => policy.RequireAuthenticatedUser());
});
```

### 11.3 CORS

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTrustedOrigins", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});
```

### 11.4 Rate Limiting

Fixed-window rate limiting per IP address: 100 requests per minute.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
```

### 11.5 Security Headers

Applied via middleware to every response:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    await next();
});
```

| Header | Purpose |
|--------|---------|
| `X-Content-Type-Options: nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options: DENY` | Prevents clickjacking via iframes |
| `Referrer-Policy` | Limits referrer information leakage |
| `X-XSS-Protection` | Legacy XSS filter (defense-in-depth) |

### 11.6 Additional Measures

- **HTTPS Redirection** — `app.UseHttpsRedirection()` redirects HTTP → HTTPS
- **HSTS** — `app.UseHsts()` in production tells browsers to only use HTTPS
- **Input Validation** — `[Required]`, `[StringLength]`, `[Range]` attributes on all DTOs
- **Parameterized Queries** — All ADO.NET queries use `Parameters.AddWithValue()` (no string concatenation)
- **No Sensitive Data in Errors** — `app.UseExceptionHandler()` in production hides stack traces
- **Unused Endpoints Removed** — Default WeatherForecast controller has been removed

---

## 12. Program.cs — DI & Middleware

```csharp
using System.Threading.RateLimiting;
using dot_net_core_rest_api.Data;
using dot_net_core_rest_api.Repositories;
using dot_net_core_rest_api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string not configured.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

// DI — Repository → Service
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
builder.Services.AddScoped<ISubCategoryService, SubCategoryService>();

// JWT Authentication + Authorization + CORS + Rate Limiting (see Security section)
// ...

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Middleware pipeline order matters:
app.UseHttpsRedirection();
app.UseCors("AllowTrustedOrigins");
app.UseRateLimiter();
app.UseAuthentication();       // Must come before UseAuthorization
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("fixed");

app.Run();
```

---

## 13. Environment Variables & Secrets

All secrets must be provided via environment variables — never committed to source control.

| Variable | Purpose | Example |
|----------|---------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=...;Password=...` |
| `Jwt__Key` | JWT signing key (min 32 characters) | `my-super-secret-key-at-least-32-chars` |
| `Jwt__Issuer` | Token issuer claim | `dot-net-core-rest-api` |
| `Jwt__Audience` | Token audience claim | `dot-net-core-rest-api-clients` |
| `Cors__AllowedOrigins__0` | First allowed CORS origin | `https://myapp.com` |
| `Cors__AllowedOrigins__1` | Second allowed CORS origin | `https://admin.myapp.com` |

> **Note:** ASP.NET Core maps `__` (double underscore) in environment variables to `:` in configuration keys.

### Local Development — User Secrets

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=secret"
dotnet user-secrets set "Jwt:Key" "my-local-dev-jwt-key-thats-at-least-32-characters-long"
```

### Docker

```bash
docker run -d -p 8080:8080 \
  -e "ConnectionStrings__DefaultConnection=Host=host.docker.internal;Port=5432;Database=mydb;Username=postgres;Password=secret" \
  -e "Jwt__Key=my-production-jwt-key-thats-at-least-32-characters-long" \
  dot-net-core-rest-api
```

### IIS — Environment Variables

Set in IIS Manager → Application → Configuration Editor → `system.webServer/aspNetCore` → `environmentVariables`, or in `web.config`:

```xml
<aspNetCore processPath="dotnet" arguments=".\dot-net-core-rest-api.dll">
  <environmentVariables>
    <environmentVariable name="ConnectionStrings__DefaultConnection" value="Host=..." />
    <environmentVariable name="Jwt__Key" value="..." />
  </environmentVariables>
</aspNetCore>
```

---

## 14. Run the Application

```bash
# Run in development mode
dotnet run

# Run with hot reload
dotnet watch run
```

The API will be available at `http://localhost:5101`.

---

## 15. API Endpoints

### Categories (EF Core)

| Method | URL | Auth | Description | Request Body |
|--------|-----|------|-------------|--------------|
| `GET` | `/api/categories` | Public | Get all categories | — |
| `GET` | `/api/categories/{id}` | Public | Get by ID | — |
| `POST` | `/api/categories` | JWT | Create category | `{ "code": "ELEC", "name": "Electronics" }` |
| `PUT` | `/api/categories/{id}` | JWT | Update category | `{ "code": "UPD", "name": "Updated" }` |
| `DELETE` | `/api/categories/{id}` | JWT | Delete category | — |

### SubCategories (ADO.NET)

| Method | URL | Auth | Description | Request Body |
|--------|-----|------|-------------|--------------|
| `GET` | `/api/subcategories` | Public | Get all | — |
| `GET` | `/api/subcategories/{id}` | Public | Get by ID | — |
| `GET` | `/api/subcategories/by-category/{categoryId}` | Public | Get by category | — |
| `POST` | `/api/subcategories` | JWT | Create | `{ "code": "PHONE", "name": "Phones", "categoryId": 1 }` |
| `PUT` | `/api/subcategories/{id}` | JWT | Update | `{ "name": "Updated" }` |
| `DELETE` | `/api/subcategories/{id}` | JWT | Delete | — |

### Test with curl

```bash
# Public — no auth needed
curl http://localhost:5101/api/categories
curl http://localhost:5101/api/subcategories/by-category/1

# Protected — requires JWT
curl -X POST http://localhost:5101/api/categories \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"code": "ELEC", "name": "Electronics"}'
```

---

## 16. API Verification — Swagger / OpenAPI & Postman

### OpenAPI (Swagger)

The API exposes an OpenAPI document at `/openapi/v1.json` in development mode.

```bash
# Start the API
dotnet run

# Fetch the OpenAPI spec
curl http://localhost:5101/openapi/v1.json -o openapi.json
```

To browse the API interactively with Swagger UI, add the `Swashbuckle.AspNetCore` package:

```bash
dotnet add package Swashbuckle.AspNetCore
```

Then in `Program.cs`:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// After app.Build():
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Browse to `http://localhost:5101/swagger`.

### Postman

1. Import the OpenAPI spec: **File → Import → URL** → `http://localhost:5101/openapi/v1.json`
2. Postman auto-generates a collection with all endpoints
3. Set up an environment variable `{{baseUrl}}` = `http://localhost:5101`
4. For protected endpoints, add `Authorization: Bearer {{token}}` in the collection-level headers

### VS Code REST Client

Use the included `dot-net-core-rest-api.http` file with the [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension.

---

## 17. API Specification for Frontend Team

### Generate OpenAPI JSON

```bash
dotnet run &
curl http://localhost:5101/openapi/v1.json -o docs/openapi.json
```

### Generate Client SDK (TypeScript)

Use [openapi-generator](https://openapi-generator.tech/) or [NSwag](https://github.com/RicoSuter/NSwag):

```bash
# Using openapi-generator-cli (npm)
npx @openapitools/openapi-generator-cli generate \
  -i http://localhost:5101/openapi/v1.json \
  -g typescript-axios \
  -o ./frontend-sdk

# Using NSwag (.NET tool)
dotnet tool install -g NSwag.ConsoleCore
nswag openapi2tsclient /input:http://localhost:5101/openapi/v1.json /output:api-client.ts
```

### Share with Frontend Team

Provide the team with:
1. **`openapi.json`** — Machine-readable API spec
2. **`api-client.ts`** — Generated TypeScript SDK (typed request/response models)
3. **Postman Collection** — Exported from Postman (File → Export → Collection v2.1)

---

## 18. Unit Testing

### Test Structure

| Test File | Layer | Technique | Tests |
|-----------|-------|-----------|-------|
| `CategoriesControllerTests.cs` | Controller | Mocks `ICategoryService` | 8 |
| `CategoryServiceTests.cs` | Service | Mocks `ICategoryRepository` + `ILogger` | 9 |
| `CategoryRepositoryTests.cs` | Repository | EF Core InMemory database | 7 |
| `SubCategoriesControllerTests.cs` | Controller | Mocks `ISubCategoryService` | 10 |
| `SubCategoryServiceTests.cs` | Service | Mocks `ISubCategoryRepository` + `ILogger` | 12 |
| `SubCategoryRepositoryTests.cs` | Repository | Testcontainers (real PostgreSQL) | 10 |
| `DtoTests.cs` | DTOs | Record equality, deconstruct, ToString | 12 |

### Testing Strategy per Layer

- **Controller** — Mock the service interface. Assert HTTP status codes (`Ok`, `NotFound`, `CreatedAtAction`, `NoContent`).
- **Service** — Mock the repository interface. Assert DTO mapping, partial update logic, null handling, and that repository methods are invoked.
- **Repository (EF)** — Use `UseInMemoryDatabase` for fast, isolated tests.
- **Repository (ADO.NET)** — Use **Testcontainers** to spin up a real PostgreSQL Docker container for accurate testing.

### Run Tests

```bash
cd Tests

# Run all tests
dotnet test

# Run only unit tests (exclude integration)
dotnet test --filter "FullyQualifiedName!~Integration&FullyQualifiedName!~SubCategoryRepository"

# Run a specific test class
dotnet test --filter "CategoryServiceTests"

# Run a specific test method
dotnet test --filter "GetById_NonExistingId_ReturnsNotFound"
```

---

## 19. Integration Testing

Integration tests use `WebApplicationFactory` with **Testcontainers** to start the full ASP.NET Core application with a real PostgreSQL database in Docker.

### IntegrationTestFactory

```csharp
public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Create tables via raw SQL
        // Attach JWT token to HttpClient for authenticated endpoints
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestToken());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _container.GetConnectionString());
        builder.UseSetting("Jwt:Key", TestJwtKey);
        // Replace DbContext and NpgsqlDataSource registrations
    }
}
```

### Run Integration Tests

```bash
# Requires Docker Desktop running
cd Tests

# Run all integration tests
dotnet test --filter "Integration"

# Run SubCategory integration tests only
dotnet test --filter "SubCategoriesIntegrationTests"
```

---

## 20. Code Coverage Report

```bash
cd Tests

# 1. Run tests with coverage collection
dotnet test /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput=./coverage/ \
  /p:Include="[dot-net-core-rest-api]*"

# 2. Install report generator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# 3. Generate HTML report
reportgenerator \
  -reports:./coverage/coverage.cobertura.xml \
  -targetdir:./coverage/report \
  -reporttypes:Html

# 4. Open the report
start ./coverage/report/index.html    # Windows
open ./coverage/report/index.html     # macOS
xdg-open ./coverage/report/index.html # Linux
```

**Coverage exclusions** (configured in the test `.csproj`):
- `Program.cs` — entry point / configuration, not unit-testable
- `GeneratedCodeAttribute` / `CompilerGeneratedAttribute` — auto-generated code

---

## 21. Deployment — IIS

### Prerequisites

- Windows Server with IIS installed
- [.NET 10 Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed
- IIS **ASP.NET Core Module (ANCM)** enabled

### Steps

1. **Publish the application:**

   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Create IIS Site:**
   - Open IIS Manager → Add Website
   - **Physical Path:** point to the `publish` folder
   - **Binding:** set port (e.g., 80 or 443 with SSL certificate)

3. **Configure Application Pool:**
   - Set **.NET CLR Version** to **No Managed Code** (ASP.NET Core runs out-of-process)
   - Set **Start Mode** to **AlwaysRunning** for production

4. **Set Environment Variables:**
   - In IIS Manager → Site → Configuration Editor → `system.webServer/aspNetCore` → `environmentVariables`
   - Add `ConnectionStrings__DefaultConnection`, `Jwt__Key`, etc.

5. **Verify:**

   ```bash
   curl https://your-server/api/categories
   ```

### `web.config` (auto-generated by `dotnet publish`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" />
    </handlers>
    <aspNetCore processPath="dotnet"
                arguments=".\dot-net-core-rest-api.dll"
                stdoutLogEnabled="false"
                hostingModel="InProcess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

---

## 22. Deployment — Docker

### Dockerfile

Multi-stage build: SDK (build) → Runtime-only (final).

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
RUN dotnet build "./dot-net-core-rest-api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./dot-net-core-rest-api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dot-net-core-rest-api.dll"]
```

### Build & Run

```bash
# Build the image
docker build -t dot-net-core-rest-api .

# Run with environment variables
docker run -d -p 8080:8080 \
  -e "ConnectionStrings__DefaultConnection=Host=host.docker.internal;Port=5432;Database=mydb;Username=postgres;Password=secret" \
  -e "Jwt__Key=production-jwt-signing-key-at-least-32-chars" \
  -e "Jwt__Issuer=dot-net-core-rest-api" \
  -e "Jwt__Audience=dot-net-core-rest-api-clients" \
  --name api \
  dot-net-core-rest-api

# Verify
curl http://localhost:8080/api/categories
```

### Docker Compose (with PostgreSQL)

```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=appdb;Username=postgres;Password=postgres"
      Jwt__Key: "production-jwt-signing-key-at-least-32-chars"
      Jwt__Issuer: "dot-net-core-rest-api"
      Jwt__Audience: "dot-net-core-rest-api-clients"
    depends_on:
      - db

  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: appdb
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

```bash
docker compose up -d
```

---

## 23. Design Patterns Used

| Pattern | Where | Purpose |
|---------|-------|---------|
| **Repository Pattern** | `ICategoryRepository` / `CategoryRepository`, `ISubCategoryRepository` / `SubCategoryRepository` | Abstracts data access; allows swapping EF Core ↔ ADO.NET without changing upper layers |
| **Service Layer Pattern** | `ICategoryService` / `CategoryService`, `ISubCategoryService` / `SubCategoryService` | Contains business logic; maps between Entities and DTOs |
| **Dependency Injection** | Constructor injection in all layers via `Program.cs` registrations | Loose coupling; testability via mocking interfaces |
| **DTO Pattern** | `CategoryDto`, `CreateCategoryRequest`, `UpdateCategoryRequest`, `SubCategoryDto`, etc. | Separates API contract from internal entities; controls what data is exposed |
| **Interface Segregation** | All layers depend on interfaces (`ICategoryService`, `ICategoryRepository`) | Enables unit testing with mocks; decouples implementation details |
| **Fluent API Configuration** | `CategoryConfiguration` (EF Core `IEntityTypeConfiguration<T>`) | Maps C# PascalCase to PostgreSQL snake_case; defines constraints and indexes |
| **Factory Pattern** | `IntegrationTestFactory` (`WebApplicationFactory<Program>`) | Spins up the full app + database for integration testing |
| **Primary Constructor** | All services, repositories, and controllers | Reduces boilerplate; cleaner constructor injection syntax (C# 12+) |

---

## 24. SQL Migrations

### `Migrations/000_create_categories.sql`

```sql
CREATE TABLE IF NOT EXISTS categories (
    id          SERIAL PRIMARY KEY,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    code        VARCHAR     NOT NULL UNIQUE,
    name        VARCHAR     NOT NULL UNIQUE
);
```

### `Migrations/001_create_sub_categories.sql`

```sql
CREATE TABLE IF NOT EXISTS sub_categories (
    id          SERIAL PRIMARY KEY,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    code        VARCHAR     NOT NULL UNIQUE,
    name        VARCHAR     NOT NULL UNIQUE,
    category_id INT         NOT NULL REFERENCES categories(id) ON DELETE CASCADE
);
```

Run these against your database manually or via a migration runner.

---

## 25. Supabase Connection Notes

| Issue | Solution |
|---|---|
| `XX000: Tenant or user not found` | Verify the **Host** matches exactly. Copy from Supabase Dashboard → Settings → Database → Connection Pooling. |
| `42P01: relation "x" does not exist` | PostgreSQL is case-sensitive. Ensure `ToTable("name")` matches the exact table name. |
| `ObjectDisposedException` | Add `No Reset On Close=true;Multiplexing=false` to the connection string (required for PgBouncer). |
| Verify table name | `SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';` |

---

## License

This project is for learning purposes.
