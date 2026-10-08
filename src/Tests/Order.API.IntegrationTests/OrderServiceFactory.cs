using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Order.API.IntegrationTests;

/// <summary>
/// Boots Order.API in-process against a throwaway PostgreSQL container.
/// Override the image with ORDER_TESTS_POSTGRES_IMAGE (e.g. a registry mirror).
/// </summary>
public sealed class OrderServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(
            Environment.GetEnvironmentVariable("ORDER_TESTS_POSTGRES_IMAGE") ?? "postgres:16-alpine")
        .WithDatabase("orderdb")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
    }
}
