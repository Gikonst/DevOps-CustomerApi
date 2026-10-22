using CustomerApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Infrastructure.Persistence;

public sealed class CustomerDbContext(DbContextOptions<CustomerDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var customer = modelBuilder.Entity<Customer>();
        customer.ToTable("Customers");
        customer.HasKey(entity => entity.CustomerId);
        customer.Property(entity => entity.CustomerId).HasMaxLength(32).ValueGeneratedNever();
        customer.Property(entity => entity.FirstName).HasMaxLength(Customer.MaxNameLength).IsRequired();
        customer.Property(entity => entity.CustomerName).HasMaxLength(Customer.MaxNameLength).IsRequired();
        customer.Property(entity => entity.Age).IsRequired();
        customer.Property(entity => entity.Gender).HasConversion<string>().HasMaxLength(20).IsRequired();
    }
}
