using CustomerApi.Application.Interfaces;
using CustomerApi.Domain.Entities;
using CustomerApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Infrastructure.Repositories;

public sealed class CustomerRepository(CustomerDbContext dbContext) : ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        await dbContext.Customers.AsNoTracking()
            .OrderBy(customer => customer.CustomerId)
            .Skip(checked((page - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<Customer?> GetByIdAsync(string customerId, CancellationToken cancellationToken) =>
        dbContext.Customers.SingleOrDefaultAsync(customer => customer.CustomerId == customerId, cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken) =>
        await dbContext.Customers.AddAsync(customer, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public void Remove(Customer customer) => dbContext.Customers.Remove(customer);
}
