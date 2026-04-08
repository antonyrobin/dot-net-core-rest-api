# 🔷 .NET 9 Web API — Backend Services

> **Role in Project:** Backend microservices powering the billing software platform
> **Version:** .NET 9.0 / C# 13
> **Repository:** `billing-backend`
> **Related:** [YARP Gateway](./yarp-api-gateway.md) | [PostgreSQL](./postgresql.md) | [EF Core + DbUp](./ef-core-dbup.md)

---

## Table of Contents

1. [Purpose & Overview](#1-purpose--overview)
2. [Why We Chose .NET 9](#2-why-we-chose-net-9)
3. [Advantages & Disadvantages](#3-advantages--disadvantages)
4. [Prerequisites](#4-prerequisites)
5. [Installation & Setup](#5-installation--setup)
6. [Project Creation](#6-project-creation)
7. [Solution & Project Structure](#7-solution--project-structure)
8. [Development Guide](#8-development-guide)
9. [API Design & Controllers](#9-api-design--controllers)
10. [Entity Framework Core](#10-entity-framework-core)
11. [Authentication & Authorization](#11-authentication--authorization)
12. [Multi-Tenancy with RLS](#12-multi-tenancy-with-rls)
13. [Dependency Injection](#13-dependency-injection)
14. [SOLID Principles in .NET](#14-solid-principles-in-net)
15. [Error Handling & Validation](#15-error-handling--validation)
16. [Do's & Don'ts](#16-dos--donts)
17. [Testing](#17-testing)
18. [Performance & Optimization](#18-performance--optimization)
19. [How to Run](#19-how-to-run)
20. [Local Deployment](#20-local-deployment)
21. [Cloud Deployment with Docker](#21-cloud-deployment-with-docker)
22. [Troubleshooting](#22-troubleshooting)
23. [Useful Commands](#23-useful-commands)
24. [References](#24-references)

---

## 1. Purpose & Overview

**.NET 9 Web API** is Microsoft's framework for building high-performance HTTP APIs. It provides:

- **Minimal APIs & Controllers** — Choose between lightweight minimal APIs or structured controllers
- **Built-in DI** — First-class dependency injection container
- **Middleware Pipeline** — Composable request/response pipeline
- **OpenAPI/Swagger** — Auto-generated API documentation
- **Cross-platform** — Runs on Windows, Linux, macOS, and containers

### Role in This Project

.NET 9 powers our **4 consolidated microservices**:

| Service | Responsibilities |
|---|---|
| **Identity Service** | Auth, Users, Tenants, Branches, Config, Feature Flags, RBAC |
| **Catalog Service** | Products, Categories, Inventory, Stock, Barcode, Procurement, Files |
| **Commerce Service** | Orders, Cart, Billing, Payments, Tax/GST, Discounts, Delivery, Accounts & Ledger |
| **Engagement Service** | Email, SMS, Push, WhatsApp, Reviews, Reports, AI, Support |

### How It Fits in the Architecture

```
Clients → Cloudflare → YARP Gateway → .NET 9 Services → PostgreSQL / Redis / RabbitMQ
                                          │
                                          ├── Identity Service (:8081)
                                          ├── Catalog Service  (:8082)
                                          ├── Commerce Service (:8083)
                                          └── Engagement Service (:8084)
```

---

## 2. Why We Chose .NET 9

| Factor | Decision Rationale |
|---|---|
| **Performance** | #1 in TechEmpower benchmarks; handles 7M+ req/sec in plaintext |
| **Type Safety** | C# strongly typed; catches bugs at compile time |
| **Mature Ecosystem** | Entity Framework Core, MassTransit, FluentValidation, Polly |
| **Native AOT** | Ahead-of-Time compilation for fast container startup |
| **YARP Integration** | API Gateway in same ecosystem — no polyglot overhead |
| **Long-term Support** | Microsoft-backed; .NET 9 = STS, but upgrade path to .NET 10 LTS is seamless |
| **Docker-friendly** | Official `mcr.microsoft.com` images; small Linux containers |
| **Multi-tenancy** | EF Core + PostgreSQL RLS integration for tenant isolation |

---

## 3. Advantages & Disadvantages

### ✅ Advantages

| # | Advantage | Detail |
|---|---|---|
| 1 | **Extreme Performance** | Kestrel web server; span-based memory; zero-allocation patterns |
| 2 | **C# Language Features** | Records, pattern matching, null safety, async streams, primary constructors |
| 3 | **Entity Framework Core** | Powerful ORM; LINQ queries; migrations; PostgreSQL support |
| 4 | **Dependency Injection** | Built-in DI container; no third-party needed |
| 5 | **Middleware Pipeline** | Composable; each concern isolated |
| 6 | **Swagger/OpenAPI** | Auto-generated interactive API docs |
| 7 | **Health Checks** | Built-in health check framework for K8s probes |
| 8 | **Background Services** | `IHostedService` for background jobs (email sending, report generation) |
| 9 | **Cross-platform** | Same code runs on Windows (dev) and Linux (prod/Docker) |
| 10 | **Observability** | Built-in support for OpenTelemetry, structured logging |

### ❌ Disadvantages

| # | Disadvantage | Mitigation |
|---|---|---|
| 1 | **Learning Curve** | C# / .NET ecosystem requires training → extensive docs and courses |
| 2 | **Memory Usage** | Higher than Go/Rust for simple services → acceptable for our workload size |
| 3 | **Cold Start** | JIT compilation on first request → use Native AOT or ReadyToRun for containers |
| 4 | **Ecosystem Lock-in** | Tied to Microsoft tools → .NET is open-source; runs on Linux |
| 5 | **EF Core N+1** | ORM can generate suboptimal queries → use `.Include()`, raw SQL for complex queries |

---

## 4. Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| **.NET SDK** | 9.0 | Framework and CLI tools |
| **Visual Studio 2022 / VS Code** | Latest | IDE |
| **Docker** | 24.x | Local PostgreSQL, Redis, RabbitMQ |
| **Git** | 2.x | Version control |
| **PostgreSQL** | 16 | Database (via Docker) |

### VS Code Extensions

```
ms-dotnettools.csharp              # C# language support (powered by OmniSharp)
ms-dotnettools.csdevkit            # C# Dev Kit (solution explorer, test explorer)
ms-dotnettools.vscode-dotnet-runtime # .NET Runtime installer
patcx.vscode-nuget-gallery         # NuGet package search
humao.rest-client                  # REST client for API testing
```

---

## 5. Installation & Setup

### Install .NET 9 SDK (Windows)

```powershell
# Option 1: Download from https://dotnet.microsoft.com/download/dotnet/9.0

# Option 2: Using winget
winget install Microsoft.DotNet.SDK.9

# Verify
dotnet --version   # 9.0.xxx
dotnet --list-sdks # List all installed SDKs
```

### Install Global Tools

```powershell
# Entity Framework Core CLI
dotnet tool install --global dotnet-ef

# Code formatter
dotnet tool install --global dotnet-format

# HTTPS development certificate
dotnet dev-certs https --trust

# Verify
dotnet ef --version
```

### Configure User Secrets (Development)

```powershell
# Initialize user secrets for a project
cd src/Services/Identity/Identity.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=billing;Username=billing_user;Password=dev_password"
dotnet user-secrets set "Jwt:PrivateKey" "your-dev-private-key"
```

---

## 6. Project Creation

### Create the Solution Structure

```powershell
# Create solution
mkdir billing-backend
cd billing-backend
dotnet new sln -n billing-backend

# Create shared library
dotnet new classlib -n Billing.Shared -o src/Shared/Billing.Shared

# Create Gateway
dotnet new web -n BillingGateway -o src/Gateway/BillingGateway

# Create Services
dotnet new webapi -n Identity.API -o src/Services/Identity/Identity.API
dotnet new webapi -n Catalog.API -o src/Services/Catalog/Catalog.API
dotnet new webapi -n Commerce.API -o src/Services/Commerce/Commerce.API
dotnet new webapi -n Engagement.API -o src/Services/Engagement/Engagement.API

# Create test projects
dotnet new xunit -n Identity.Tests -o tests/Identity.Tests
dotnet new xunit -n Catalog.Tests -o tests/Catalog.Tests
dotnet new xunit -n Commerce.Tests -o tests/Commerce.Tests
dotnet new xunit -n Engagement.Tests -o tests/Engagement.Tests

# Create Migration project
dotnet new console -n DatabaseMigrator -o src/Migrations/DatabaseMigrator

# Add all to solution
dotnet sln add src/Shared/Billing.Shared
dotnet sln add src/Gateway/BillingGateway
dotnet sln add src/Services/Identity/Identity.API
dotnet sln add src/Services/Catalog/Catalog.API
dotnet sln add src/Services/Commerce/Commerce.API
dotnet sln add src/Services/Engagement/Engagement.API
dotnet sln add src/Migrations/DatabaseMigrator
dotnet sln add tests/Identity.Tests
dotnet sln add tests/Catalog.Tests
dotnet sln add tests/Commerce.Tests
dotnet sln add tests/Engagement.Tests

# Add project references
dotnet add src/Services/Identity/Identity.API reference src/Shared/Billing.Shared
dotnet add src/Services/Catalog/Catalog.API reference src/Shared/Billing.Shared
dotnet add src/Services/Commerce/Commerce.API reference src/Shared/Billing.Shared
dotnet add src/Services/Engagement/Engagement.API reference src/Shared/Billing.Shared
```

### Install Core NuGet Packages

```powershell
# Shared library packages
cd src/Shared/Billing.Shared
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package MassTransit.RabbitMQ
dotnet add package StackExchange.Redis
dotnet add package FluentValidation
dotnet add package Serilog.AspNetCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# Per service (example — Identity)
cd src/Services/Identity/Identity.API
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package AspNetCore.HealthChecks.NpgSql
dotnet add package AspNetCore.HealthChecks.Redis
dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Swashbuckle.AspNetCore
```

---

## 7. Solution & Project Structure

```
billing-backend/
├── billing-backend.sln
│
├── src/
│   ├── Gateway/
│   │   └── BillingGateway/
│   │       ├── Program.cs
│   │       ├── Middleware/
│   │       │   ├── CorrelationIdMiddleware.cs
│   │       │   ├── RateLimitingMiddleware.cs
│   │       │   └── RequestLoggingMiddleware.cs
│   │       └── appsettings.json
│   │
│   ├── Services/
│   │   ├── Identity/
│   │   │   └── Identity.API/
│   │   │       ├── Program.cs
│   │   │       ├── Modules/
│   │   │       │   ├── Auth/
│   │   │       │   │   ├── AuthController.cs
│   │   │       │   │   ├── AuthService.cs
│   │   │       │   │   ├── Dtos/
│   │   │       │   │   │   ├── LoginRequest.cs
│   │   │       │   │   │   ├── VerifyOtpRequest.cs
│   │   │       │   │   │   └── TokenResponse.cs
│   │   │       │   │   ├── Validators/
│   │   │       │   │   │   └── LoginRequestValidator.cs
│   │   │       │   │   └── Entities/
│   │   │       │   │       └── RefreshToken.cs
│   │   │       │   ├── Users/
│   │   │       │   │   ├── UserController.cs
│   │   │       │   │   ├── UserService.cs
│   │   │       │   │   ├── Dtos/
│   │   │       │   │   └── Entities/
│   │   │       │   │       └── User.cs
│   │   │       │   ├── Tenants/
│   │   │       │   ├── Config/
│   │   │       │   └── RBAC/
│   │   │       ├── Data/
│   │   │       │   ├── IdentityDbContext.cs
│   │   │       │   └── Configurations/  # EF Core entity configs
│   │   │       └── appsettings.json
│   │   │
│   │   ├── Catalog/
│   │   │   └── Catalog.API/
│   │   │       ├── Modules/
│   │   │       │   ├── Products/
│   │   │       │   ├── Categories/
│   │   │       │   ├── Inventory/
│   │   │       │   ├── Barcode/
│   │   │       │   ├── Procurement/
│   │   │       │   └── Files/
│   │   │       └── Data/
│   │   │
│   │   ├── Commerce/
│   │   │   └── Commerce.API/
│   │   │       ├── Modules/
│   │   │       │   ├── Orders/
│   │   │       │   ├── Billing/
│   │   │       │   ├── Payments/
│   │   │       │   ├── Tax/
│   │   │       │   ├── Accounts/
│   │   │       │   └── Delivery/
│   │   │       └── Data/
│   │   │
│   │   └── Engagement/
│   │       └── Engagement.API/
│   │           ├── Modules/
│   │           │   ├── Notifications/
│   │           │   ├── Reviews/
│   │           │   ├── Reports/
│   │           │   ├── AI/
│   │           │   └── Support/
│   │           └── Data/
│   │
│   ├── Shared/
│   │   └── Billing.Shared/
│   │       ├── Domain/
│   │       │   ├── BaseEntity.cs
│   │       │   ├── IAuditableEntity.cs
│   │       │   └── ITenantEntity.cs
│   │       ├── Infrastructure/
│   │       │   ├── BaseDbContext.cs
│   │       │   ├── TenantInterceptor.cs
│   │       │   └── RedisService.cs
│   │       ├── Security/
│   │       │   ├── JwtService.cs
│   │       │   └── TenantContext.cs
│   │       └── Contracts/
│   │           ├── ProductCreated.cs
│   │           └── OrderPlaced.cs
│   │
│   └── Migrations/
│       └── DatabaseMigrator/
│
├── tests/
│   ├── Identity.Tests/
│   ├── Catalog.Tests/
│   ├── Commerce.Tests/
│   └── Engagement.Tests/
│
├── .github/workflows/
├── docker-compose.yml
├── docker-compose.override.yml
└── Directory.Build.props
```

---

## 8. Development Guide

### 8.1 Program.cs (Service Entry Point)

```csharp
// src/Services/Catalog/Catalog.API/Program.cs
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging ----------
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ---------- Database ----------
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Redis ----------
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "billing:catalog:";
});

// ---------- Authentication ----------
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new RsaSecurityKey(RsaKeyLoader.LoadPublicKey(
                builder.Configuration["Jwt:PublicKeyPath"]!)),
        };
    });

builder.Services.AddAuthorization();

// ---------- Services (DI) ----------
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProcurementService, ProcurementService>();

// ---------- FluentValidation ----------
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---------- MassTransit (RabbitMQ) ----------
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });
        cfg.ConfigureEndpoints(context);
    });
});

// ---------- Health Checks ----------
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

// ---------- Middleware Pipeline ----------
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### 8.2 Base Entity

```csharp
// src/Shared/Billing.Shared/Domain/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}
```

### 8.3 Entity Definition

```csharp
// src/Services/Catalog/Catalog.API/Modules/Products/Entities/Product.cs
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal GstRate { get; set; }
    public Guid CategoryId { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Category Category { get; set; } = null!;
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
}
```

### 8.4 DTOs and Mapping

```csharp
// src/Services/Catalog/Catalog.API/Modules/Products/Dtos/ProductDto.cs
public record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    decimal GstRate,
    Guid CategoryId,
    string CategoryName,
    string? Description,
    string? ImageUrl,
    bool IsActive,
    int StockQuantity
);

public record CreateProductRequest(
    string Name,
    string Sku,
    decimal Price,
    decimal GstRate,
    Guid CategoryId,
    string? Description
);

public record UpdateProductRequest(
    string? Name,
    decimal? Price,
    decimal? GstRate,
    string? Description,
    bool? IsActive
);

// Mapping extension
public static class ProductMappings
{
    public static ProductDto ToDto(this Product product) => new(
        product.Id,
        product.Name,
        product.Sku,
        product.Price,
        product.GstRate,
        product.CategoryId,
        product.Category?.Name ?? "",
        product.Description,
        product.ImageUrl,
        product.IsActive,
        product.InventoryItems.Sum(i => i.Quantity)
    );
}
```

---

## 9. API Design & Controllers

### Controller Pattern

```csharp
// src/Services/Catalog/Catalog.API/Modules/Products/ProductController.cs
[ApiController]
[Route("api/v{version:apiVersion}/catalog/products")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<ProductDto>>(200)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _productService.GetProductsAsync(page, pageSize, search, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
    {
        var product = await _productService.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [ProducesResponseType<ProductDto>(201)]
    [ProducesResponseType<ValidationProblemDetails>(400)]
    [Authorize(Policy = "products:write")]
    public async Task<IActionResult> CreateProduct(
        CreateProductRequest request,
        CancellationToken ct)
    {
        var product = await _productService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "products:write")]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        UpdateProductRequest request,
        CancellationToken ct)
    {
        var product = await _productService.UpdateAsync(id, request, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "products:delete")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct)
    {
        var deleted = await _productService.SoftDeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
```

### Service Layer

```csharp
// src/Services/Catalog/Catalog.API/Modules/Products/ProductService.cs
public class ProductService : IProductService
{
    private readonly CatalogDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        CatalogDbContext db,
        IDistributedCache cache,
        IPublishEndpoint publishEndpoint,
        ILogger<ProductService> logger)
    {
        _db = db;
        _cache = cache;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ProductDto>> GetProductsAsync(
        int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.InventoryItems)
            .Where(p => !p.IsDeleted)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{search}%") ||
                EF.Functions.ILike(p.Sku, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.ToDto())
            .ToListAsync(ct);

        return new PaginatedResponse<ProductDto>(items, totalCount, page, pageSize);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        var product = new Product
        {
            Name = request.Name,
            Sku = request.Sku,
            Price = request.Price,
            GstRate = request.GstRate,
            CategoryId = request.CategoryId,
            Description = request.Description
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        // Publish event to message bus
        await _publishEndpoint.Publish(new ProductCreated(product.Id, product.Name, product.Sku), ct);

        _logger.LogInformation("Product created: {ProductId} {ProductName}", product.Id, product.Name);

        return product.ToDto();
    }
}
```

---

## 10. Entity Framework Core

### DbContext Configuration

```csharp
// src/Services/Catalog/Catalog.API/Data/CatalogDbContext.cs
public class CatalogDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");

        // Apply all entity configurations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // Global query filter for soft delete + tenant isolation
        modelBuilder.Entity<Product>().HasQueryFilter(
            p => !p.IsDeleted && p.TenantId == _tenantContext.TenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Auto-set audit fields
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _tenantContext.TenantId;
                entry.Entity.CreatedBy = _tenantContext.UserId.ToString();
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedBy = _tenantContext.UserId.ToString();
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(ct);
    }
}
```

### Entity Configuration (Fluent API)

```csharp
// src/Services/Catalog/Catalog.API/Data/Configurations/ProductConfiguration.cs
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();
        builder.HasIndex(p => p.TenantId);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.GstRate).HasColumnType("decimal(5,2)");
        builder.Property(p => p.Description).HasMaxLength(1000);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);
    }
}
```

---

## 11. Authentication & Authorization

### JWT Validation

```csharp
// JWT is validated by YARP Gateway (upstream).
// Services receive verified claims via X-Tenant-Id, X-User-Id, X-User-Roles headers.
// However, services ALSO validate JWT as defense-in-depth.

// Shared: TenantContext extracts tenant info from JWT or headers
public class TenantContext : ITenantContext
{
    public Guid TenantId { get; }
    public Guid UserId { get; }
    public string[] Roles { get; }
    public string[] Permissions { get; }

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        var claims = httpContextAccessor.HttpContext?.User.Claims;
        TenantId = Guid.Parse(claims?.FirstOrDefault(c => c.Type == "tid")?.Value ?? Guid.Empty.ToString());
        UserId = Guid.Parse(claims?.FirstOrDefault(c => c.Type == "sub")?.Value ?? Guid.Empty.ToString());
        Roles = claims?.Where(c => c.Type == "roles").Select(c => c.Value).ToArray() ?? [];
        Permissions = claims?.Where(c => c.Type == "perms").Select(c => c.Value).ToArray() ?? [];
    }
}
```

### Permission-based Authorization

```csharp
// Register policies in Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("products:read", policy =>
        policy.RequireClaim("perms", "products:read"));
    options.AddPolicy("products:write", policy =>
        policy.RequireClaim("perms", "products:write"));
    options.AddPolicy("products:delete", policy =>
        policy.RequireClaim("perms", "products:delete"));
});
```

---

## 12. Multi-Tenancy with RLS

### How RLS Works with EF Core

```
1. User logs in → JWT contains tenant_id (tid claim)
2. YARP Gateway validates JWT → forwards to service
3. Service extracts tid from JWT → sets TenantContext.TenantId
4. EF Core global query filter adds WHERE tenant_id = @tid to ALL queries
5. PostgreSQL RLS provides database-level enforcement (defense-in-depth)
```

### RLS Interceptor

```csharp
// Before each query, set the PostgreSQL session variable for RLS
public class TenantInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
        CancellationToken ct = default)
    {
        await base.ConnectionOpeningAsync(connection, eventData, result, ct);

        if (connection.State == ConnectionState.Open)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SET app.current_tenant_id = '{_tenantContext.TenantId}'";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        return result;
    }
}
```

---

## 13. Dependency Injection

### Registration Patterns

```csharp
// Program.cs — Services registration
// Scoped: new instance per HTTP request (most services)
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Singleton: one instance for entire app lifetime
builder.Services.AddSingleton<IRedisService, RedisService>();

// Transient: new instance every time it's requested
builder.Services.AddTransient<IValidator<CreateProductRequest>, CreateProductRequestValidator>();

// Register all validators from assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

### DI Lifetime Guide

| Lifetime | Use When | Example |
|---|---|---|
| **Scoped** | Per-request state; DB context; tenant context | `DbContext`, `ITenantContext`, `IProductService` |
| **Singleton** | Stateless or thread-safe shared state | `IRedisService`, `IConfiguration`, `HttpClient` |
| **Transient** | Lightweight, stateless utilities | Validators, mappers |

---

## 14. SOLID Principles in .NET

### S — Single Responsibility

```csharp
// ✅ Each class has ONE job
public class ProductController { /* HTTP routing only */ }
public class ProductService { /* business logic only */ }
public class ProductRepository { /* data access only */ }
public class ProductValidator { /* validation only */ }
public class ProductMapper { /* DTO mapping only */ }

// ❌ Avoid: Controller that queries DB, validates, maps, and sends emails
```

### O — Open/Closed

```csharp
// ✅ Open for extension via interfaces and generics
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<T>> GetAllAsync(CancellationToken ct);
    Task AddAsync(T entity, CancellationToken ct);
}

// New entities extend without modifying the base
public class ProductRepository : IRepository<Product> { /* ... */ }
public class CategoryRepository : IRepository<Category> { /* ... */ }
```

### L — Liskov Substitution

```csharp
// ✅ Any INotificationSender can replace another
public interface INotificationSender
{
    Task SendAsync(string recipient, string message, CancellationToken ct);
}

public class EmailSender : INotificationSender { /* ... */ }
public class SmsSender : INotificationSender { /* ... */ }
public class WhatsAppSender : INotificationSender { /* ... */ }

// Consumer doesn't care which implementation is used
public class OrderService
{
    private readonly IEnumerable<INotificationSender> _senders;
    // Can use any combination of senders
}
```

### I — Interface Segregation

```csharp
// ❌ Fat interface
public interface IProductService
{
    Task<Product> GetByIdAsync(Guid id);
    Task<Product> CreateAsync(CreateProductRequest request);
    Task DeleteAsync(Guid id);
    Task<Report> GenerateReport();  // Not related to product CRUD
    Task SendNotification();         // Not related to product
}

// ✅ Segregated interfaces
public interface IProductReader
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PaginatedResponse<ProductDto>> GetAllAsync(int page, int pageSize, CancellationToken ct);
}

public interface IProductWriter
{
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct);
}
```

### D — Dependency Inversion

```csharp
// ✅ High-level modules depend on abstractions
// Controller → IProductService (interface)
// ProductService → IRepository<Product> (interface)
// ProductService → IDistributedCache (interface)
// ProductService → IPublishEndpoint (interface)

// All wired via DI container — swap implementations without changing business logic
builder.Services.AddScoped<IProductService, ProductService>();
// For testing:
builder.Services.AddScoped<IProductService, MockProductService>();
```

---

## 15. Error Handling & Validation

### Global Exception Handler

```csharp
// src/Shared/Billing.Shared/Infrastructure/GlobalExceptionHandler.cs
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            NotFoundException => (404, "Resource not found"),
            ValidationException => (400, "Validation failed"),
            UnauthorizedAccessException => (401, "Unauthorized"),
            _ => (500, "An unexpected error occurred")
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        }, ct);

        return true;
    }
}
```

### FluentValidation

```csharp
// src/Services/Catalog/Catalog.API/Modules/Products/Validators/CreateProductRequestValidator.cs
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200);

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50)
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("SKU must be uppercase alphanumeric with dashes");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be positive");

        RuleFor(x => x.GstRate)
            .Must(rate => new[] { 0m, 5m, 12m, 18m, 28m }.Contains(rate))
            .WithMessage("GST rate must be 0, 5, 12, 18, or 28");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");
    }
}

// Register validation filter in Program.cs
builder.Services.AddControllers(options =>
    options.Filters.Add<ValidationFilter>());
```

---

## 16. Do's & Don'ts

### ✅ Do's

| # | Practice | Reason |
|---|---|---|
| 1 | **Use async/await everywhere** | All I/O operations must be async for scalability |
| 2 | **Pass CancellationToken** | Allow request cancellation; prevent wasted work |
| 3 | **Use records for DTOs** | Immutable; value equality; concise syntax |
| 4 | **Use global query filters** | Tenant isolation + soft delete applied automatically |
| 5 | **Validate at controller boundary** | FluentValidation before business logic runs |
| 6 | **Use structured logging (Serilog)** | `_logger.LogInformation("Created {ProductId}", id)` |
| 7 | **Use `AsNoTracking()` for reads** | Faster queries when you don't need change tracking |
| 8 | **Return `IActionResult` from controllers** | Flexible status code responses |
| 9 | **Use `Directory.Build.props`** | Centralize package versions and settings across all projects |
| 10 | **Use health checks** | K8s liveness/readiness probes need `/health` endpoint |

### ❌ Don'ts

| # | Anti-pattern | Correct Approach |
|---|---|---|
| 1 | **Don't use sync I/O** | Always `await` DB/HTTP/file operations |
| 2 | **Don't catch `Exception` broadly** | Catch specific exceptions; let global handler deal with the rest |
| 3 | **Don't expose entities to API** | Use DTOs; never return entity objects directly |
| 4 | **Don't hardcode connection strings** | Use `appsettings.json` + User Secrets + env vars |
| 5 | **Don't use `HttpContext` in services** | Inject `ITenantContext` or pass needed values explicitly |
| 6 | **Don't ignore migrations** | Use EF Core migrations in dev; DbUp versioned scripts in prod |
| 7 | **Don't use `DateTime.Now`** | Use `DateTime.UtcNow` — always store UTC |
| 8 | **Don't skip `[Authorize]`** | Every endpoint must have explicit auth/permissions |
| 9 | **Don't use string concatenation for SQL** | Use parameterized queries to prevent SQL injection |
| 10 | **Don't register DbContext as Singleton** | DbContext must be Scoped (per-request) |

---

## 17. Testing

### Unit Test (Service Layer)

```csharp
// tests/Catalog.Tests/ProductServiceTests.cs
public class ProductServiceTests
{
    private readonly Mock<CatalogDbContext> _mockDb;
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<IPublishEndpoint> _mockPublisher;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _mockDb = new Mock<CatalogDbContext>();
        _mockCache = new Mock<IDistributedCache>();
        _mockPublisher = new Mock<IPublishEndpoint>();
        _sut = new ProductService(
            _mockDb.Object, _mockCache.Object,
            _mockPublisher.Object, Mock.Of<ILogger<ProductService>>());
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsProductDto()
    {
        var request = new CreateProductRequest("Rice", "RIC-001", 50m, 5m, Guid.NewGuid(), null);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        Assert.Equal("Rice", result.Name);
        Assert.Equal("RIC-001", result.Sku);
        _mockPublisher.Verify(p => p.Publish(
            It.IsAny<ProductCreated>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Test (WebApplicationFactory)

```csharp
// tests/Catalog.Tests/IntegrationTests/ProductApiTests.cs
public class ProductApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real DB with in-memory for testing
                services.RemoveAll<DbContextOptions<CatalogDbContext>>();
                services.AddDbContext<CatalogDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/catalog/products");
        response.EnsureSuccessStatusCode();
    }
}
```

---

## 18. Performance & Optimization

| Technique | Implementation |
|---|---|
| **AsNoTracking** | Use for all read-only queries |
| **Compiled Queries** | `EF.CompileQuery` for hot-path queries |
| **Response Caching** | `[ResponseCache]` attribute for stable data |
| **Redis Caching** | Cache product lists, configs, tenant settings |
| **Pagination** | Always paginate list endpoints; never return all records |
| **Select Projection** | Use `.Select()` to fetch only needed columns |
| **Connection Pooling** | Npgsql connection pooling (default enabled) |
| **Output Caching** | .NET 9 output caching middleware |
| **Native AOT** | For Gateway/Identity service — 10ms cold start |
| **Bulk Operations** | `EFCore.BulkExtensions` for batch inserts/updates |

---

## 19. How to Run

### Development Mode

```powershell
cd billing-backend

# Start infrastructure (PostgreSQL, Redis, RabbitMQ)
docker compose up -d postgres redis rabbitmq

# Run database migrations
dotnet run --project src/Migrations/DatabaseMigrator

# Run a specific service
dotnet run --project src/Services/Catalog/Catalog.API

# Run the gateway
dotnet run --project src/Gateway/BillingGateway

# Swagger UI available at: https://localhost:5001/swagger
```

### Run All Services

```powershell
# Using docker compose (all services)
docker compose up

# Or run individually in separate terminals
dotnet run --project src/Gateway/BillingGateway          # :5000
dotnet run --project src/Services/Identity/Identity.API   # :8081
dotnet run --project src/Services/Catalog/Catalog.API     # :8082
dotnet run --project src/Services/Commerce/Commerce.API   # :8083
dotnet run --project src/Services/Engagement/Engagement.API # :8084
```

---

## 20. Local Deployment

### Build for Release

```powershell
# Build all projects
dotnet build -c Release

# Publish specific service
dotnet publish src/Services/Catalog/Catalog.API -c Release -o ./publish/catalog

# Run published output
dotnet ./publish/catalog/Catalog.API.dll
```

---

## 21. Cloud Deployment with Docker

### Dockerfile (Multi-stage)

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY billing-backend.sln .
COPY src/Shared/Billing.Shared/*.csproj src/Shared/Billing.Shared/
COPY src/Services/Catalog/Catalog.API/*.csproj src/Services/Catalog/Catalog.API/

# Restore dependencies
RUN dotnet restore src/Services/Catalog/Catalog.API

# Copy source code
COPY . .

# Build
RUN dotnet publish src/Services/Catalog/Catalog.API -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Catalog.API.dll"]
```

### Docker Compose (Full Stack)

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: billing
      POSTGRES_USER: billing_user
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    ports: ["5432:5432"]
    volumes: [postgres_data:/var/lib/postgresql/data]

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports: ["5672:5672", "15672:15672"]

  gateway:
    build:
      context: .
      dockerfile: src/Gateway/BillingGateway/Dockerfile
    ports: ["5000:8080"]
    depends_on: [identity, catalog, commerce, engagement]

  identity:
    build:
      context: .
      dockerfile: src/Services/Identity/Identity.API/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=billing;Username=billing_user;Password=${DB_PASSWORD}"
      ConnectionStrings__Redis: "redis:6379"
    depends_on: [postgres, redis, rabbitmq]

  catalog:
    build:
      context: .
      dockerfile: src/Services/Catalog/Catalog.API/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=billing;Username=billing_user;Password=${DB_PASSWORD}"
    depends_on: [postgres, redis, rabbitmq]

  commerce:
    build:
      context: .
      dockerfile: src/Services/Commerce/Commerce.API/Dockerfile
    depends_on: [postgres, redis, rabbitmq]

  engagement:
    build:
      context: .
      dockerfile: src/Services/Engagement/Engagement.API/Dockerfile
    depends_on: [postgres, redis, rabbitmq]

volumes:
  postgres_data:
```

---

## 22. Troubleshooting

| Issue | Cause | Fix |
|---|---|---|
| **Port already in use** | Another service on same port | Change `ASPNETCORE_URLS` or kill conflicting process |
| **EF Core migration error** | Missing migration | Run `dotnet ef migrations add <name>` |
| **Connection refused (DB)** | PostgreSQL not running | Start with `docker compose up postgres` |
| **401 Unauthorized** | Missing/expired JWT | Check token; verify clock skew settings |
| **RLS blocking queries** | TenantId not set | Verify `TenantInterceptor` runs before queries |
| **Hot reload not working** | `dotnet watch` needed | Use `dotnet watch run` instead of `dotnet run` |
| **Package restore fails** | NuGet source issues | Run `dotnet nuget locals all --clear` |

---

## 23. Useful Commands

```powershell
# Build
dotnet build                           # Build entire solution
dotnet build -c Release                # Build in Release mode
dotnet publish -c Release -o ./out     # Publish for deployment

# Run
dotnet run --project <path>            # Run specific project
dotnet watch run --project <path>      # Run with hot reload

# Entity Framework
dotnet ef migrations add <name>        # Create migration
dotnet ef database update              # Apply migrations
dotnet ef migrations script            # Generate SQL script

# Testing
dotnet test                            # Run all tests
dotnet test --filter "FullyQualifiedName~ProductService"  # Filter tests
dotnet test --collect:"XPlat Code Coverage"                # Coverage

# NuGet
dotnet add package <name>             # Add NuGet package
dotnet list package --outdated        # Check outdated packages
dotnet restore                         # Restore all packages

# Tooling
dotnet tool list --global              # List global tools
dotnet format                          # Format code
dotnet user-secrets set "Key" "Value"  # Set user secret
```

---

## 24. References

| Resource | URL |
|---|---|
| **Official Docs** | https://learn.microsoft.com/aspnet/core |
| **.NET 9 What's New** | https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9 |
| **EF Core Docs** | https://learn.microsoft.com/ef/core |
| **C# Language Reference** | https://learn.microsoft.com/dotnet/csharp |
| **MassTransit** | https://masstransit.io |
| **FluentValidation** | https://docs.fluentvalidation.net |
| **Serilog** | https://serilog.net |
| **Npgsql** | https://www.npgsql.org/efcore |
| **TechEmpower Benchmarks** | https://www.techempower.com/benchmarks |
| **.NET Docker Images** | https://hub.docker.com/_/microsoft-dotnet |
