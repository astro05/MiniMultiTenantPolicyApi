using MiniMultiTenantPolicyApi.Data;
using MiniMultiTenantPolicyApi.Services;

namespace MiniMultiTenantPolicyApi.Tests;

public class CustomerPolicyServiceTests
{
    [Fact]
    public async Task GetCustomerPolicy_WhenCustomerBelongsToTenant_ReturnsPolicy()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var service = new CustomerPolicyService(db, new FakeTenantContext(tenantId: 1));

        var result = await service.GetCustomerPolicyAsync(customerId: 101, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(101, result.CustomerId);
        Assert.Equal(1001, result.PolicyId);
        Assert.Equal("POL-ACME-001", result.PolicyNumber);
        Assert.Equal(1250.00m, result.PremiumAmount);
        Assert.Equal(new DateOnly(2026, 8, 31), result.ExpirationDate);
    }

    [Fact]
    public async Task GetCustomerPolicy_WhenCustomerBelongsToAnotherTenant_ReturnsNotFound()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var service = new CustomerPolicyService(db, new FakeTenantContext(tenantId: 2));

        var result = await service.GetCustomerPolicyAsync(customerId: 101, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCustomerPolicy_WhenCustomerDoesNotExist_ReturnsNull()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var service = new CustomerPolicyService(db, new FakeTenantContext(tenantId: 1));

        var result = await service.GetCustomerPolicyAsync(customerId: 9999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCustomerPolicy_WhenTenantDoesNotExist_ReturnsNull()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var service = new CustomerPolicyService(db, new FakeTenantContext(tenantId: 99));

        var result = await service.GetCustomerPolicyAsync(customerId: 101, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCustomerPolicy_TenantTwoCanAccessItsOwnCustomerPolicy()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var service = new CustomerPolicyService(db, new FakeTenantContext(tenantId: 2));

        var result = await service.GetCustomerPolicyAsync(customerId: 201, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2001, result.PolicyId);
        Assert.Equal("POL-GLOBEX-001", result.PolicyNumber);
    }
}
