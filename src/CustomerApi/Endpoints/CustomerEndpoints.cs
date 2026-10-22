using CustomerApi.Application.Interfaces;
using CustomerApi.Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CustomerApi.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/customers").WithTags("Customers");
        group.MapGet("/", GetAllAsync).WithName("GetCustomers");
        group.MapGet("/{customerId}", GetByIdAsync).WithName("GetCustomer");
        group.MapPost("/", CreateAsync).WithName("CreateCustomer");
        group.MapPut("/{customerId}", UpdateAsync).WithName("UpdateCustomer");
        group.MapDelete("/{customerId}", DeleteAsync).WithName("DeleteCustomer");
        return endpoints;
    }

    private static async Task<Results<Ok<IReadOnlyList<CustomerDto>>, ValidationProblem>> GetAllAsync(
        ICustomerService service, CancellationToken cancellationToken, int page = 1, int pageSize = 50)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["pagination"] = ["Page must be positive, pageSize must be between 1 and 100, and the offset must not exceed 2147483647."]
            });
        }

        return TypedResults.Ok(await service.GetAllAsync(page, pageSize, cancellationToken));
    }

    private static async Task<Results<Ok<CustomerDto>, NotFound>> GetByIdAsync(
        string customerId, ICustomerService service, CancellationToken cancellationToken)
    {
        var customer = await service.GetByIdAsync(customerId, cancellationToken);
        return customer is null ? TypedResults.NotFound() : TypedResults.Ok(customer);
    }

    private static async Task<Created<CustomerDto>> CreateAsync(
        CustomerRequest request, ICustomerService service, CancellationToken cancellationToken)
    {
        var customer = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/customers/{customer.CustomerId}", customer);
    }

    private static async Task<Results<Ok<CustomerDto>, NotFound>> UpdateAsync(
        string customerId, CustomerRequest request, ICustomerService service, CancellationToken cancellationToken)
    {
        var customer = await service.UpdateAsync(customerId, request, cancellationToken);
        return customer is null ? TypedResults.NotFound() : TypedResults.Ok(customer);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        string customerId, ICustomerService service, CancellationToken cancellationToken) =>
        await service.DeleteAsync(customerId, cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound();
}
