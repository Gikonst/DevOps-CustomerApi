using System.Net;
using CustomerApi.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerApi.IntegrationTests;

public class HealthEndpointsTests(CustomerApiFactory factory) : IClassFixture<CustomerApiFactory>
{
    [Fact]
    public async Task Health_WhenDatabaseIsAvailable_ReturnsHealthy()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Live_ReturnsHealthyWithoutCheckingDatabase()
    {
        var response = await factory.CreateClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_WhenTableIsMissing_ReturnsServiceUnavailable()
    {
        using var isolated = new CustomerApiFactory();
        using (var scope = isolated.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
            await db.Database.EnsureDeletedAsync();
        }

        var response = await isolated.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
