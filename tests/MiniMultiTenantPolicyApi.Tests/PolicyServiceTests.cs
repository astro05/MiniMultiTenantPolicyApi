using MiniMultiTenantPolicyApi.Data;
using MiniMultiTenantPolicyApi.Models;
using MiniMultiTenantPolicyApi.Services;

namespace MiniMultiTenantPolicyApi.Tests;

public class PolicyServiceTests
{
    [Fact]
    public async Task GetExpiringPolicies_ReturnsOnlyPoliciesOfTheCurrentTenant()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var service = new PolicyService(db, new FakeTenantContext(tenantId: 1), TestDates.Clock);

        var result = await service.GetExpiringPoliciesAsync(withinDays: 30, CancellationToken.None);

        var policy = Assert.Single(result);
        Assert.Equal(1001, policy.PolicyId);
        Assert.Equal(101, policy.CustomerId);
        Assert.DoesNotContain(result, p => p.PolicyId == 2001);
    }

    [Fact]
    public async Task GetExpiringPolicies_WhenTenantTwoRequests_ReturnsOnlyTenantTwoPolicies()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var service = new PolicyService(db, new FakeTenantContext(tenantId: 2), TestDates.Clock);

        var result = await service.GetExpiringPoliciesAsync(withinDays: 30, CancellationToken.None);

        var policy = Assert.Single(result);
        Assert.Equal(2001, policy.PolicyId);
    }

    [Fact]
    public async Task GetExpiringPolicies_ExcludesAlreadyExpiredPolicies()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        db.Policies.Add(new Policy
        {
            Id = 3001,
            CustomerId = 101,
            PolicyNumber = "POL-ACME-EXPIRED",
            ExpirationDate = TestDates.Today.AddDays(-1),
            PremiumAmount = 100m
        });
        await db.SaveChangesAsync();
        var service = new PolicyService(db, new FakeTenantContext(tenantId: 1), TestDates.Clock);

        var result = await service.GetExpiringPoliciesAsync(withinDays: 30, CancellationToken.None);

        Assert.DoesNotContain(result, p => p.PolicyId == 3001);
    }

    [Fact]
    public async Task GetExpiringPolicies_ExcludesPoliciesBeyondTheWindow()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var service = new PolicyService(db, new FakeTenantContext(tenantId: 1), TestDates.Clock);

        var result = await service.GetExpiringPoliciesAsync(withinDays: 30, CancellationToken.None);

        Assert.DoesNotContain(result, p => p.PolicyId == 1002);
    }

    [Fact]
    public async Task GetExpiringPolicies_IncludesPoliciesExpiringOnTheWindowBoundaries()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        db.Policies.AddRange(
            new Policy
            {
                Id = 3002,
                CustomerId = 101,
                PolicyNumber = "POL-ACME-TODAY",
                ExpirationDate = TestDates.Today,
                PremiumAmount = 100m
            },
            new Policy
            {
                Id = 3003,
                CustomerId = 101,
                PolicyNumber = "POL-ACME-LAST-DAY",
                ExpirationDate = TestDates.Today.AddDays(30),
                PremiumAmount = 100m
            });
        await db.SaveChangesAsync();
        var service = new PolicyService(db, new FakeTenantContext(tenantId: 1), TestDates.Clock);

        var result = await service.GetExpiringPoliciesAsync(withinDays: 30, CancellationToken.None);

        Assert.Contains(result, p => p.PolicyId == 3002);
        Assert.Contains(result, p => p.PolicyId == 3003);
    }

    [Fact]
    public async Task GetPolicyExpiration_ReturnsTheExpirationDateStoredInTheDatabase()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var service = new PolicyService(db, new FakeTenantContext(tenantId: 1), TestDates.Clock);

        var result = await service.GetPolicyExpirationAsync(policyId: 1001, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1001, result.PolicyId);
        Assert.Equal("POL-ACME-001", result.PolicyNumber);
        Assert.Equal(new DateOnly(2026, 8, 31), result.ExpirationDate);
    }

    [Fact]
    public async Task GetPolicyExpiration_WhenPolicyBelongsToAnotherTenant_ReturnsNull()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var service = new PolicyService(db, new FakeTenantContext(tenantId: 2), TestDates.Clock);

        var result = await service.GetPolicyExpirationAsync(policyId: 1001, CancellationToken.None);

        Assert.Null(result);
    }
}
