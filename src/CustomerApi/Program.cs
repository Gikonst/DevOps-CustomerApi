using System.Text.Json.Serialization;
using CustomerApi.Application.Interfaces;
using CustomerApi.Application.Services;
using CustomerApi.Endpoints;
using CustomerApi.ExceptionHandling;
using CustomerApi.Health;
using CustomerApi.Infrastructure.Persistence;
using CustomerApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Customers");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings:Customers using user secrets or the ConnectionStrings__Customers environment variable.");
}

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BadHttpRequestExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddDbContext<CustomerDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddHealthChecks().AddCheck<CustomerDatabaseHealthCheck>("customers-database");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CustomerApi v1");
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.MapCustomerEndpoints();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });

app.Run();

public partial class Program;
