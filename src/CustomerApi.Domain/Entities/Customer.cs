namespace CustomerApi.Domain.Entities;

public class Customer
{
    public const int MaxNameLength = 100;
    public const int MaxAge = 150;

    public string CustomerId { get; private set; } = Guid.NewGuid().ToString("N");
    public string FirstName { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public int Age { get; private set; }
    public Gender Gender { get; private set; }

    private Customer()
    {
    }

    public Customer(string firstName, string customerName, int age, Gender gender)
    {
        Update(firstName, customerName, age, gender);
    }

    public void Update(string firstName, string customerName, int age, Gender gender)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(firstName.Length, MaxNameLength, nameof(firstName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(customerName.Length, MaxNameLength, nameof(customerName));
        ArgumentOutOfRangeException.ThrowIfNegative(age);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(age, MaxAge);
        if (!Enum.IsDefined(gender))
        {
            throw new ArgumentOutOfRangeException(nameof(gender), gender, "Gender must be a defined value.");
        }

        FirstName = firstName.Trim();
        CustomerName = customerName.Trim();
        Age = age;
        Gender = gender;
    }
}
