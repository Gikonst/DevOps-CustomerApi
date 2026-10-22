using CustomerApi.Domain.Entities;

namespace CustomerApi.UnitTests.Domain;

public class CustomerTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsPropertiesAndGeneratesId()
    {
        var customer = new Customer("Jane", "Doe", 30, Gender.Female);

        Assert.Equal("Jane", customer.FirstName);
        Assert.Equal("Doe", customer.CustomerName);
        Assert.Equal(30, customer.Age);
        Assert.Equal(Gender.Female, customer.Gender);
        Assert.Equal(32, customer.CustomerId.Length);
    }

    [Fact]
    public void Constructor_GeneratesUniqueIds()
    {
        var first = new Customer("Jane", "Doe", 30, Gender.Female);
        var second = new Customer("Jane", "Doe", 30, Gender.Female);

        Assert.NotEqual(first.CustomerId, second.CustomerId);
    }

    [Fact]
    public void Constructor_TrimsNames()
    {
        var customer = new Customer("  Jane ", " Doe  ", 30, Gender.Female);

        Assert.Equal("Jane", customer.FirstName);
        Assert.Equal("Doe", customer.CustomerName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(Customer.MaxAge)]
    public void Constructor_AcceptsBoundaryAges(int age)
    {
        var customer = new Customer("Jane", "Doe", age, Gender.Female);

        Assert.Equal(age, customer.Age);
    }

    [Fact]
    public void Constructor_AcceptsMaximumNameLength()
    {
        var name = new string('a', Customer.MaxNameLength);

        var customer = new Customer(name, name, 1, Gender.Male);

        Assert.Equal(name, customer.FirstName);
        Assert.Equal(name, customer.CustomerName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankFirstName_Throws(string? firstName)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Customer(firstName!, "Doe", 30, Gender.Male));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankCustomerName_Throws(string? customerName)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Customer("Jane", customerName!, 30, Gender.Male));
    }

    [Fact]
    public void Constructor_WithTooLongFirstName_Throws()
    {
        var name = new string('a', Customer.MaxNameLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Customer(name, "Doe", 30, Gender.Male));
    }

    [Fact]
    public void Constructor_WithTooLongCustomerName_Throws()
    {
        var name = new string('a', Customer.MaxNameLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Customer("Jane", name, 30, Gender.Male));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(Customer.MaxAge + 1)]
    public void Constructor_WithInvalidAge_Throws(int age)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Customer("Jane", "Doe", age, Gender.Male));
    }

    [Fact]
    public void Constructor_WithUndefinedGender_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Customer("Jane", "Doe", 30, (Gender)99));
    }

    [Fact]
    public void Update_ReplacesValuesAndKeepsId()
    {
        var customer = new Customer("Jane", "Doe", 30, Gender.Female);
        var id = customer.CustomerId;

        customer.Update("John", "Smith", 40, Gender.Male);

        Assert.Equal(id, customer.CustomerId);
        Assert.Equal("John", customer.FirstName);
        Assert.Equal("Smith", customer.CustomerName);
        Assert.Equal(40, customer.Age);
        Assert.Equal(Gender.Male, customer.Gender);
    }

    [Fact]
    public void Update_WithInvalidValues_LeavesCustomerUnchanged()
    {
        var customer = new Customer("Jane", "Doe", 30, Gender.Female);

        Assert.Throws<ArgumentOutOfRangeException>(() => customer.Update("John", "Smith", -5, Gender.Male));

        Assert.Equal("Jane", customer.FirstName);
        Assert.Equal("Doe", customer.CustomerName);
        Assert.Equal(30, customer.Age);
        Assert.Equal(Gender.Female, customer.Gender);
    }
}
