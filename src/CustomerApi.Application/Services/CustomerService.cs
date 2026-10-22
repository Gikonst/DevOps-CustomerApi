using System.ComponentModel.DataAnnotations;
using CustomerApi.Application.Interfaces;
using CustomerApi.Application.Mappers;
using CustomerApi.Application.Models;

namespace CustomerApi.Application.Services;

public sealed class CustomerService(ICustomerRepository repository) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((long)(page - 1) * pageSize, int.MaxValue, nameof(page));
        var customers = await repository.GetAllAsync(page, pageSize, cancellationToken);
        return customers.Select(customer => customer.ToDto()).ToArray();
    }

    public async Task<CustomerDto?> GetByIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetByIdAsync(customerId, cancellationToken);
        return customer?.ToDto();
    }

    public async Task<CustomerDto> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var customer = request.ToEntity();
        await repository.AddAsync(customer, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return customer.ToDto();
    }

    public async Task<CustomerDto?> UpdateAsync(string customerId, CustomerRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var customer = await repository.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        request.MapTo(customer);
        await repository.SaveChangesAsync(cancellationToken);
        return customer.ToDto();
    }

    public async Task<bool> DeleteAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return false;
        }

        repository.Remove(customer);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Validate(CustomerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
    }
}
