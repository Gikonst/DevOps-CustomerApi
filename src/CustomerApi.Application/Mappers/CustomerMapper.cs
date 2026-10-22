using CustomerApi.Application.Models;
using CustomerApi.Domain.Entities;

namespace CustomerApi.Application.Mappers;

public static class CustomerMapper
{
    public static CustomerDto ToDto(this Customer customer) =>
        new(customer.CustomerId, customer.FirstName, customer.CustomerName, customer.Age, customer.Gender);

    public static Customer ToEntity(this CustomerRequest request) =>
        new(request.FirstName, request.CustomerName, request.Age, request.Gender);

    public static void MapTo(this CustomerRequest request, Customer customer) =>
        customer.Update(request.FirstName, request.CustomerName, request.Age, request.Gender);
}
