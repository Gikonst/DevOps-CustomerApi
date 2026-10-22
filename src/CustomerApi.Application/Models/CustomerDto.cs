using CustomerApi.Domain.Entities;

namespace CustomerApi.Application.Models;

public sealed record CustomerDto(string CustomerId, string FirstName, string CustomerName, int Age, Gender Gender);
