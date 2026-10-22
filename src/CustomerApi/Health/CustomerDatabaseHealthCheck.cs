using CustomerApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CustomerApi.Health;

public sealed class CustomerDatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        // Query the mapped table so a reachable database with missing migrations is not marked ready.
        await dbContext.Customers.Select(customer => customer.CustomerId).Take(1).ToListAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
