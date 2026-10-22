using CustomerApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CustomerApi.IntegrationTests;

/// <summary>
/// Hosts the real API pipeline but swaps PostgreSQL for an in-memory SQLite database,
/// so the tests run without external infrastructure.
/// </summary>
public sealed class CustomerApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CustomerApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs requires a connection string at startup; the value is replaced below.
        builder.UseSetting("ConnectionStrings:Customers", "Host=unused");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CustomerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CustomerDbContext>>();
            services.AddDbContext<CustomerDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CustomerDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
