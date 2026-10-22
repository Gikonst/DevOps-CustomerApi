using CustomerApi.Domain.Entities;

namespace CustomerApi.Application.Interfaces;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<Customer?> GetByIdAsync(string customerId, CancellationToken cancellationToken);
    Task AddAsync(Customer customer, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void Remove(Customer customer);
}
