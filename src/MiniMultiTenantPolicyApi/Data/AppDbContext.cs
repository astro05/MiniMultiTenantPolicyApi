using Microsoft.EntityFrameworkCore;
using MiniMultiTenantPolicyApi.Models;

namespace MiniMultiTenantPolicyApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Policy> Policies => Set<Policy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);

            entity.HasMany(t => t.Customers)
                .WithOne(c => c.Tenant)
                .HasForeignKey(c => c.TenantId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);

            entity.HasIndex(c => c.TenantId);

            entity.HasMany(c => c.Policies)
                .WithOne(p => p.Customer)
                .HasForeignKey(p => p.CustomerId);
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.Property(p => p.PolicyNumber).IsRequired().HasMaxLength(50);

            entity.HasIndex(p => p.CustomerId);
            entity.HasIndex(p => p.ExpirationDate);

            entity.ToTable(tb => tb.HasCheckConstraint(
                "CK_Policy_PremiumAmount_NonNegative",
                "\"PremiumAmount\" >= 0"));
        });
    }
}
