using System.ComponentModel.DataAnnotations;
using CustomerApi.Domain.Entities;

namespace CustomerApi.Application.Models;

public sealed class CustomerRequest
{
    [Required, StringLength(Customer.MaxNameLength)]
    public required string FirstName { get; init; }

    [Required, StringLength(Customer.MaxNameLength)]
    public required string CustomerName { get; init; }

    [Range(0, Customer.MaxAge)]
    public required int Age { get; init; }

    [EnumDataType(typeof(Gender))]
    public required Gender Gender { get; init; }
}
