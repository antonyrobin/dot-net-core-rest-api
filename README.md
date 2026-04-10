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
- [26. API Standards](#26-api-standards)
- [27. Performance Optimizations](#27-performance-optimizations)
- [28. Structured Logging (Serilog)](#28-structured-logging-serilog)

---

## Project Structure

```
dot-net-core-rest-api/
├── Constants/
│   └── ErrorTypes.cs                     # Centralized RFC 7807 error type URIs
├── Controllers/
│   ├── BaseApiController.cs              # Shared response helpers (ApiOk, ApiNotFound, etc.)
│   ├── CategoriesController.cs           # Category endpoints (JWT-protected)
│   └── SubCategoriesController.cs        # SubCategory endpoints (JWT-protected)
├── Data/
│   ├── AppDbContext.cs                    # EF Core DbContext
│   ├── IUnitOfWork.cs                    # Unit of Work interface (transaction abstraction)
│   ├── UnitOfWork.cs                     # Unit of Work implementation (Npgsql transactions)
│   └── Configurations/
│       └── CategoryConfiguration.cs      # Fluent API table mapping
├── Dtos/
│   ├── CategoryDtos.cs                   # Category request/response records (validated)
│   └── SubCategoryDtos.cs               # SubCategory request/response records (validated)
├── Entities/
│   ├── Category.cs                       # Category domain model
│   └── SubCategory.cs                    # SubCategory domain model
├── Helpers/
│   ├── CursorHelper.cs                   # Base64 cursor encode/decode for cursor pagination
│   └── SortHelper.cs                     # SQL ORDER BY builder with column whitelist
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs      # Unhandled exception → RFC 7807 response
│   ├── RequestIdMiddleware.cs            # X-Request-Id header injection
│   └── RequestLoggingMiddleware.cs       # HTTP request/response logging
├── Migrations/
│   ├── 000_create_categories.sql         # Categories table DDL
│   └── 001_create_sub_categories.sql     # SubCategories table DDL
├── Models/
│   ├── ApiResponse.cs                    # Standardized API response envelope
│   ├── PagedResult.cs                    # Generic paged result wrapper
│   └── QueryParameters.cs               # Pagination, filtering & sorting query params
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
├── Logs/                                 # Auto-created log files (daily rolling, 30-day retention)
│   └── log-YYYYMMDD.txt
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

# Structured logging with async file sink
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Async
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

// Database — shared NpgsqlDataSource
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string not configured.");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dataSource));

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
| `GET` | `/api/v1/categories` | Public | Get all categories | — |
| `GET` | `/api/v1/categories/{id}` | Public | Get by ID | — |
| `POST` | `/api/v1/categories` | JWT | Create category | `{ "code": "ELEC", "name": "Electronics" }` |
| `PUT` | `/api/v1/categories/{id}` | JWT | Update category | `{ "code": "UPD", "name": "Updated" }` |
| `DELETE` | `/api/v1/categories/{id}` | JWT | Delete category | — |

### SubCategories (ADO.NET)

| Method | URL | Auth | Description | Request Body |
|--------|-----|------|-------------|--------------|
| `GET` | `/api/v1/sub-categories` | Public | Get all | — |
| `GET` | `/api/v1/sub-categories/{id}` | Public | Get by ID | — |
| `GET` | `/api/v1/sub-categories/by-category/{categoryId}` | Public | Get by category | — |
| `POST` | `/api/v1/sub-categories` | JWT | Create | `{ "code": "PHONE", "name": "Phones", "categoryId": 1 }` |
| `PUT` | `/api/v1/sub-categories/{id}` | JWT | Update | `{ "name": "Updated" }` |
| `DELETE` | `/api/v1/sub-categories/{id}` | JWT | Delete | — |

### Health Check

| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| `GET` | `/health` | Public | Database connectivity check |

### Test with curl

```bash
# Public — no auth needed
curl http://localhost:5101/api/v1/categories
curl http://localhost:5101/api/v1/sub-categories/by-category/1

# Public with pagination, filtering & sorting
curl "http://localhost:5101/api/v1/categories?page=1&limit=10&sort=name:asc"
curl "http://localhost:5101/api/v1/sub-categories?cursor=eyJpZCI6IDIwfQ==&limit=10"

# Health check
curl http://localhost:5101/health

# Protected — requires JWT
curl -X POST http://localhost:5101/api/v1/categories \
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
| **Unit of Work** | `IUnitOfWork` / `UnitOfWork` | Manages transactions across multiple raw SQL operations |
| **Dependency Injection** | Constructor injection in all layers via `Program.cs` registrations | Loose coupling; testability via mocking interfaces |
| **DTO Pattern** | `CategoryDto`, `CreateCategoryRequest`, `UpdateCategoryRequest`, `SubCategoryDto`, etc. | Separates API contract from internal entities; controls what data is exposed |
| **Interface Segregation** | All layers depend on interfaces (`ICategoryService`, `ICategoryRepository`) | Enables unit testing with mocks; decouples implementation details |
| **Middleware Pipeline** | `RequestIdMiddleware`, `GlobalExceptionMiddleware`, `RequestLoggingMiddleware` | Cross-cutting concerns (logging, error handling, request tracking) |
| **Base Controller** | `BaseApiController` | Shared response helpers (`ApiOk`, `ApiNotFound`, `ApiCreated`, `ApiValidationError`) |
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

## 26. API Standards

### 26.1 Standardized Response Format

All API responses follow a consistent envelope structure:

**Success Response:**

```json
{
  "success": true,
  "data": { ... },
  "meta": {
    "page": 1,
    "limit": 20,
    "total": 100,
    "cursor": "eyJpZCI6IDIwfQ==",
    "hasMore": true
  },
  "timestamp": "2025-01-15T10:30:00.000Z",
  "requestId": "0HN8..."
}
```

**Error Response (RFC 7807):**

```json
{
  "success": false,
  "error": {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
    "title": "Not Found",
    "status": 404,
    "detail": "Category with id 99 was not found.",
    "instance": "/api/v1/categories/99"
  },
  "timestamp": "2025-01-15T10:30:00.000Z",
  "requestId": "0HN8..."
}
```

### 26.2 URL Conventions

- **Versioned:** All endpoints are prefixed with `/api/v1/`
- **kebab-case:** Multi-word resources use hyphens: `/api/v1/sub-categories`
- **Nested resources:** `/api/v1/sub-categories/by-category/{categoryId}`

### 26.3 Pagination

Both **cursor-based** and **offset-based** pagination are supported:

```bash
# Offset-based (default)
GET /api/v1/categories?page=2&limit=10

# Cursor-based
GET /api/v1/categories?cursor=eyJpZCI6IDIwfQ==&limit=10
```

- Cursor pagination skips the COUNT query for better performance.
- The `cursor` value is a Base64-encoded JSON object containing the last item's ID.

### 26.4 Filtering & Sorting

```bash
# Filter by name and code
GET /api/v1/categories?name=electronics&code=ELEC

# Sort by multiple columns
GET /api/v1/categories?sort=name:asc,created_at:desc

# Combined
GET /api/v1/sub-categories?categoryId=1&sort=name:asc&page=1&limit=10
```

Sorting uses a **column whitelist** to prevent SQL injection. Only known columns are accepted.

### 26.5 Rate Limiting (Token Bucket)

Two tiers configured:

| Tier | Limit | Window | Partition |
|------|-------|--------|-----------|
| `standard` | 100 requests | 1 minute | Per IP |
| `premium` | 1000 requests | 1 minute | Per IP |

Exceeded requests return HTTP 429 with an RFC 7807 error body.

### 26.6 Error Type Constants

All error type URIs are centralized in `Constants/ErrorTypes.cs`:

```csharp
public static class ErrorTypes
{
    public const string NotFound = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
    public const string Validation = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    public const string InternalServerError = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
    public const string TooManyRequests = "https://tools.ietf.org/html/rfc6585#section-4";
}
```

### 26.7 Request ID & Logging

- Every request is tagged with a unique `X-Request-Id` header via `RequestIdMiddleware`.
- `RequestLoggingMiddleware` logs all HTTP requests at **Information** level (method, path, status, duration).
- Controller actions log at **Debug** level to avoid duplication with middleware.
- Service and repository layers log at **Debug** level for detailed tracing.
- **Warning** level is used for "not found" scenarios in controllers and services.

### 26.8 Health Check

```bash
GET /health
```

Returns database connectivity status. Available without authentication.

---

## 27. Performance Optimizations

### 27.1 Shared NpgsqlDataSource

Both EF Core (`AppDbContext`) and raw ADO.NET (`SubCategoryRepository`) share a **single** `NpgsqlDataSource` instance — one connection pool instead of two:

```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dataSource));
```

### 27.2 COUNT Skip for Cursor Pagination

When cursor-based pagination is used, the COUNT query is skipped entirely — the total is returned as `0` since cursor pagination doesn't need it:

```csharp
var total = string.IsNullOrWhiteSpace(query.Cursor) ? await q.CountAsync(ct) : 0;
```

### 27.3 Response Compression

Brotli and Gzip compression are enabled for all responses:

```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
```

### 27.4 Output Caching

Anonymous GET endpoints are cached for 60 seconds using output caching:

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.NoCache());
    options.AddPolicy("CachePublicGet", policy =>
        policy.Expire(TimeSpan.FromSeconds(60)).Tag("public"));
});
```

Controllers apply via attribute: `[OutputCache(PolicyName = "CachePublicGet")]`

### 27.5 Request Timeouts

A 30-second default timeout prevents long-running requests from consuming resources:

```csharp
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
});
```

### 27.6 Unit of Work Pattern

`IUnitOfWork` provides transaction support for raw SQL operations that need atomicity:

```csharp
await using var uow = serviceProvider.GetRequiredService<IUnitOfWork>();
await uow.BeginAsync(ct);

try
{
    // Multiple operations sharing the same connection and transaction
    await uow.CommitAsync(ct);
}
catch
{
    await uow.RollbackAsync(ct);
    throw;
}
```

Registered as `Scoped` — one unit of work per HTTP request.

---

## 28. Structured Logging (Serilog)

### 28.1 Overview

The application uses **Serilog** with an **async file sink** for structured, non-blocking log output. Logs are written to both the console and daily rolling files.

| Feature | Detail |
|---------|--------|
| Library | Serilog.AspNetCore 9.0 + Serilog.Sinks.Async 2.1 |
| Console | Always enabled |
| File location | `Logs/log-YYYYMMDD.txt` |
| Rolling | Daily (new file per day) |
| Size limit | 10 MB per file (rolls to next segment) |
| Retention | **30 days** — older files are deleted automatically |
| Async writes | Non-blocking — log calls return immediately, a background thread flushes to disk |
| Format | `{Timestamp} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}` |

### 28.2 Configuration (`appsettings.json`)

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Async" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "dot_net_core_rest_api": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "File",
              "Args": {
                "path": "Logs/log-.txt",
                "rollingInterval": "Day",
                "retainedFileCountLimit": 30,
                "fileSizeLimitBytes": 10485760,
                "rollOnFileSizeLimit": true,
                "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
              }
            }
          ]
        }
      }
    ],
    "Enrich": [ "FromLogContext" ]
  }
}
```

### 28.3 Program.cs Integration

```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog replaces the built-in logger
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
```

All existing `ILogger<T>` injections continue to work — Serilog acts as a drop-in replacement.

### 28.4 Log Retention

| Setting | Value | Effect |
|---------|-------|--------|
| `retainedFileCountLimit` | 30 | Files older than 30 days are **deleted automatically** by Serilog |
| `fileSizeLimitBytes` | 10,485,760 (10 MB) | A new segment file is created if a single day exceeds 10 MB |
| `rollOnFileSizeLimit` | true | Enables segment creation (e.g., `log-20260411.txt`, `log-20260411_001.txt`) |

### 28.5 Development Override

`appsettings.Development.json` lowers the minimum level to `Debug` for the application namespace:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "dot_net_core_rest_api": "Debug"
      }
    }
  }
}
```

### 28.6 Sample Log Output

```
2026-04-11 10:32:15.123 +05:30 [INF] [RequestLoggingMiddleware] HTTP GET /api/v1/categories started | RequestId: req_a1b2c3d4
2026-04-11 10:32:15.189 +05:30 [INF] [RequestLoggingMiddleware] HTTP GET /api/v1/categories completed 200 in 66ms | RequestId: req_a1b2c3d4
2026-04-11 10:32:15.400 +05:30 [WRN] [RequestLoggingMiddleware] HTTP GET /api/v1/categories/999 completed 404 in 12ms | RequestId: req_e5f6g7h8
```

### 28.7 Docker Considerations

In containerized deployments, logs are written to `/app/Logs/` inside the container. To persist logs, mount a volume:

```bash
docker run -v ./logs:/app/Logs your-image
```

Alternatively, rely on console output and use a log aggregation tool (ELK, Seq, Datadog).

---

## License

This project is for learning purposes.
