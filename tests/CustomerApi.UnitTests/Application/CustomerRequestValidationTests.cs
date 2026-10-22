using System.ComponentModel.DataAnnotations;
using CustomerApi.Application.Models;
using CustomerApi.Domain.Entities;

namespace CustomerApi.UnitTests.Application;

public class CustomerRequestValidationTests
{
    private static List<ValidationResult> Validate(CustomerRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static CustomerRequest Create(
        string firstName = "Jane", string customerName = "Doe", int age = 30, Gender gender = Gender.Female) =>
        new() { FirstName = firstName, CustomerName = customerName, Age = age, Gender = gender };

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        Assert.Empty(Validate(Create()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MissingFirstName_IsInvalid(string? firstName)
    {
        var results = Validate(Create(firstName: firstName!));

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CustomerRequest.FirstName)));
    }

    [Fact]
    public void TooLongCustomerName_IsInvalid()
    {
        var results = Validate(Create(customerName: new string('a', Customer.MaxNameLength + 1)));

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CustomerRequest.CustomerName)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(Customer.MaxAge + 1)]
    public void AgeOutOfRange_IsInvalid(int age)
    {
        var results = Validate(Create(age: age));

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CustomerRequest.Age)));
    }

    [Fact]
    public void UndefinedGender_IsInvalid()
    {
        var results = Validate(Create(gender: (Gender)42));

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CustomerRequest.Gender)));
    }
}
