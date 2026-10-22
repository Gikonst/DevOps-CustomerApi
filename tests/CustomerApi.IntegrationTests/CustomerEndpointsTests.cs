using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CustomerApi.Application.Models;
using CustomerApi.Domain.Entities;

namespace CustomerApi.IntegrationTests;

public class CustomerEndpointsTests(CustomerApiFactory factory) : IClassFixture<CustomerApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    private static object NewRequest(
        string firstName = "Jane", string customerName = "Doe", int age = 30, string gender = "Female") =>
        new { firstName, customerName, age, gender };

    private async Task<CustomerDto> CreateCustomerAsync(object? request = null)
    {
        var response = await _client.PostAsJsonAsync("/customers", request ?? NewRequest());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);
        return Assert.IsType<CustomerDto>(dto);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithLocationAndBody()
    {
        var response = await _client.PostAsJsonAsync("/customers", NewRequest("Alice", "Wonder", 25, "Female"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Wonder", dto.CustomerName);
        Assert.Equal(25, dto.Age);
        Assert.Equal(Gender.Female, dto.Gender);
        Assert.Equal($"/customers/{dto.CustomerId}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_SerializesGenderAsString()
    {
        var response = await _client.PostAsJsonAsync("/customers", NewRequest(gender: "Male"));

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal("Male", document.RootElement.GetProperty("gender").GetString());
    }

    [Fact]
    public async Task GetById_AfterCreate_ReturnsCustomer()
    {
        var created = await CreateCustomerAsync();

        var response = await _client.GetAsync($"/customers/{created.CustomerId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created, await response.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions));
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/customers/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsCreatedCustomers()
    {
        var created = await CreateCustomerAsync();

        var response = await _client.GetAsync("/customers?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var customers = await response.Content.ReadFromJsonAsync<List<CustomerDto>>(JsonOptions);
        Assert.NotNull(customers);
        Assert.Contains(created, customers);
    }

    [Fact]
    public async Task GetAll_HonorsPaging()
    {
        await CreateCustomerAsync();
        await CreateCustomerAsync();

        var first = await _client.GetFromJsonAsync<List<CustomerDto>>("/customers?page=1&pageSize=1", JsonOptions);
        var second = await _client.GetFromJsonAsync<List<CustomerDto>>("/customers?page=2&pageSize=1", JsonOptions);

        Assert.NotNull(first);
        Assert.NotNull(second);
        var firstItem = Assert.Single(first);
        var secondItem = Assert.Single(second);
        Assert.NotEqual(firstItem.CustomerId, secondItem.CustomerId);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    public async Task GetAll_WithInvalidPaging_ReturnsBadRequest(string query)
    {
        var response = await _client.GetAsync($"/customers?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesCustomer()
    {
        var created = await CreateCustomerAsync();

        var response = await _client.PutAsJsonAsync(
            $"/customers/{created.CustomerId}", NewRequest("John", "Smith", 50, "Male"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);
        Assert.Equal(new CustomerDto(created.CustomerId, "John", "Smith", 50, Gender.Male), updated);

        var fetched = await _client.GetFromJsonAsync<CustomerDto>($"/customers/{created.CustomerId}", JsonOptions);
        Assert.Equal(updated, fetched);
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/customers/does-not-exist", NewRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesCustomer()
    {
        var created = await CreateCustomerAsync();

        var deleteResponse = await _client.DeleteAsync($"/customers/{created.CustomerId}");
        var getResponse = await _client.GetAsync($"/customers/{created.CustomerId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/customers/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public static TheoryData<string, object> InvalidRequests => new()
    {
        { "blank first name", NewRequest(firstName: "") },
        { "blank customer name", NewRequest(customerName: "") },
        { "negative age", NewRequest(age: -1) },
        { "age too large", NewRequest(age: Customer.MaxAge + 1) },
        { "name too long", NewRequest(firstName: new string('a', Customer.MaxNameLength + 1)) },
        { "unknown gender", NewRequest(gender: "Other") },
        { "numeric gender", new { firstName = "Jane", customerName = "Doe", age = 30, gender = 0 } },
        { "missing fields", new { firstName = "Jane" } }
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest(string scenario, object request)
    {
        var response = await _client.PostAsJsonAsync("/customers", request);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Scenario '{scenario}' returned {response.StatusCode}.");
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequestAndKeepsCustomer()
    {
        var created = await CreateCustomerAsync();

        var response = await _client.PutAsJsonAsync($"/customers/{created.CustomerId}", NewRequest(age: -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var fetched = await _client.GetFromJsonAsync<CustomerDto>($"/customers/{created.CustomerId}", JsonOptions);
        Assert.Equal(created, fetched);
    }
}
