using CustomerApi.Application.Mappers;
using CustomerApi.Application.Models;
using CustomerApi.Domain.Entities;

namespace CustomerApi.UnitTests.Application;

public class CustomerMapperTests
{
    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var customer = new Customer("Jane", "Doe", 30, Gender.Female);

        var dto = customer.ToDto();

        Assert.Equal(new CustomerDto(customer.CustomerId, "Jane", "Doe", 30, Gender.Female), dto);
    }

    [Fact]
    public void ToEntity_MapsRequestToNewCustomer()
    {
        var request = new CustomerRequest { FirstName = "John", CustomerName = "Smith", Age = 41, Gender = Gender.Male };

        var customer = request.ToEntity();

        Assert.Equal("John", customer.FirstName);
        Assert.Equal("Smith", customer.CustomerName);
        Assert.Equal(41, customer.Age);
        Assert.Equal(Gender.Male, customer.Gender);
        Assert.False(string.IsNullOrEmpty(customer.CustomerId));
    }

    [Fact]
    public void MapTo_UpdatesExistingCustomerKeepingId()
    {
        var customer = new Customer("Jane", "Doe", 30, Gender.Female);
        var id = customer.CustomerId;
        var request = new CustomerRequest { FirstName = "John", CustomerName = "Smith", Age = 41, Gender = Gender.Male };

        request.MapTo(customer);

        Assert.Equal(id, customer.CustomerId);
        Assert.Equal("John", customer.FirstName);
        Assert.Equal("Smith", customer.CustomerName);
        Assert.Equal(41, customer.Age);
        Assert.Equal(Gender.Male, customer.Gender);
    }
}
