using dot_net_core_rest_api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace dot_net_core_rest_api.Tests;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Create both tables (categories via EF in prod, but raw SQL here for speed)
        await using var conn = new NpgsqlConnection(_container.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS categories (
                id          SERIAL PRIMARY KEY,
                created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
                code        VARCHAR     NOT NULL UNIQUE,
                name        VARCHAR     NOT NULL UNIQUE
            );
            CREATE TABLE IF NOT EXISTS sub_categories (
                id          SERIAL PRIMARY KEY,
                created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
                code        VARCHAR     NOT NULL UNIQUE,
                name        VARCHAR     NOT NULL UNIQUE,
                category_id INT         NOT NULL REFERENCES categories(id) ON DELETE CASCADE
            );
            """, conn);
        await cmd.ExecuteNonQueryAsync();

        HttpClient = CreateClient();
    }

    public new async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = _container.GetConnectionString();

        // Provide the connection string via configuration so Program.cs doesn't throw
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            // Remove the existing NpgsqlDataSource registration
            var dataSourceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(NpgsqlDataSource));
            if (dataSourceDescriptor != null)
                services.Remove(dataSourceDescriptor);

            // Register with the Testcontainer connection string
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        });
    }
}
