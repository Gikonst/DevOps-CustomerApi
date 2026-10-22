using System.ComponentModel.DataAnnotations;
using CustomerApi.Application.Interfaces;
using CustomerApi.Application.Models;
using CustomerApi.Application.Services;
using CustomerApi.Domain.Entities;
using NSubstitute;

namespace CustomerApi.UnitTests.Application;

public class CustomerServiceTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _service = new CustomerService(_repository);
    }

    private static CustomerRequest ValidRequest() =>
        new() { FirstName = "Jane", CustomerName = "Doe", Age = 30, Gender = Gender.Female };

    [Fact]
    public async Task GetAllAsync_ReturnsMappedCustomers()
    {
        var customers = new[]
        {
            new Customer("Jane", "Doe", 30, Gender.Female),
            new Customer("John", "Smith", 41, Gender.Male)
        };
        _repository.GetAllAsync(2, 10, Arg.Any<CancellationToken>()).Returns(customers);

        var result = await _service.GetAllAsync(2, 10);

        Assert.Equal(2, result.Count);
        Assert.Equal(customers[0].CustomerId, result[0].CustomerId);
        Assert.Equal("John", result[1].FirstName);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public async Task GetAllAsync_WithInvalidPaging_Throws(int page, int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetAllAsync(page, pageSize));

        await _repository.DidNotReceiveWithAnyArgs().GetAllAsync(default, default, default);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDto()
    {
        var customer = new Customer("Jane", "Doe", 30, Gender.Female);
        _repository.GetByIdAsync(customer.CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _service.GetByIdAsync(customer.CustomerId);

        Assert.NotNull(result);
        Assert.Equal(customer.CustomerId, result.CustomerId);
        Assert.Equal("Jane", result.FirstName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repository.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((Customer?)null);

        Assert.Null(await _service.GetByIdAsync("missing"));
    }

    [Fact]
    public async Task CreateAsync_AddsAndSavesCustomer()
    {
        var result = await _service.CreateAsync(ValidRequest());

        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Doe", result.CustomerName);
        Assert.Equal(30, result.Age);
        Assert.Equal(Gender.Female, result.Gender);
        await _repository.Received(1).AddAsync(
            Arg.Is<Customer>(c => c.CustomerId == result.CustomerId), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ThrowsAndDoesNotPersist()
    {
        var request = new CustomerRequest { FirstName = "Jane", CustomerName = "Doe", Age = -1, Gender = Gender.Female };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));

        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndSaves()
    {
        var customer = new Customer("Old", "Name", 20, Gender.Male);
        _repository.GetByIdAsync(customer.CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _service.UpdateAsync(customer.CustomerId, ValidRequest());

        Assert.NotNull(result);
        Assert.Equal(customer.CustomerId, result.CustomerId);
        Assert.Equal("Jane", customer.FirstName);
        Assert.Equal(Gender.Female, customer.Gender);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNullAndDoesNotSave()
    {
        _repository.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await _service.UpdateAsync("missing", ValidRequest());

        Assert.Null(result);
        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidRequest_ThrowsBeforeLookup()
    {
        var request = new CustomerRequest { FirstName = "", CustomerName = "Doe", Age = 30, Gender = Gender.Female };

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync("id", request));

        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_RemovesAndSaves()
    {
        var customer = new Customer("Jane", "Doe", 30, Gender.Female);
        _repository.GetByIdAsync(customer.CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var deleted = await _service.DeleteAsync(customer.CustomerId);

        Assert.True(deleted);
        _repository.Received(1).Remove(customer);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ReturnsFalse()
    {
        _repository.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var deleted = await _service.DeleteAsync("missing");

        Assert.False(deleted);
        _repository.DidNotReceiveWithAnyArgs().Remove(default!);
        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
