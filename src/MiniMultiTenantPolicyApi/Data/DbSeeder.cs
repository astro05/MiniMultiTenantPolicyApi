using Microsoft.EntityFrameworkCore;
using MiniMultiTenantPolicyApi.Models;
using MiniMultiTenantPolicyApi.Services;

namespace MiniMultiTenantPolicyApi.Data;

public class DbSeeder
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public DbSeeder(AppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        var today = _clock.UtcNow.Date;

        _db.Tenants.AddRange(
            new Tenant { Id = 1, Name = "Acme Insurance" },
            new Tenant { Id = 2, Name = "Globex Insurance" });

        _db.Customers.AddRange(
            new Customer { Id = 101, TenantId = 1, Name = "Alice Johnson" },
            new Customer { Id = 102, TenantId = 1, Name = "Bob Smith" },
            new Customer { Id = 201, TenantId = 2, Name = "Charlie Brown" },
            new Customer { Id = 202, TenantId = 2, Name = "Diana Miller" });

        _db.Policies.AddRange(
            new Policy
            {
                Id = 1001,
                CustomerId = 101,
                PolicyNumber = "POL-ACME-001",
                ExpirationDate = today.AddDays(10),
                PremiumAmount = 1250.00m
            },
            new Policy
            {
                Id = 1002,
                CustomerId = 102,
                PolicyNumber = "POL-ACME-002",
                ExpirationDate = today.AddDays(45),
                PremiumAmount = 890.50m
            },
            new Policy
            {
                Id = 2001,
                CustomerId = 201,
                PolicyNumber = "POL-GLOBEX-001",
                ExpirationDate = today.AddDays(10),
                PremiumAmount = 1500.00m
            },
            new Policy
            {
                Id = 2002,
                CustomerId = 202,
                PolicyNumber = "POL-GLOBEX-002",
                ExpirationDate = today.AddDays(60),
                PremiumAmount = 720.25m
            });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
