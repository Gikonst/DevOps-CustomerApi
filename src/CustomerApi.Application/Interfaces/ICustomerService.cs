using CustomerApi.Application.Models;

namespace CustomerApi.Application.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto?> UpdateAsync(string customerId, CustomerRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string customerId, CancellationToken cancellationToken = default);
}
