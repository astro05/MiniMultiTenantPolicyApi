using Microsoft.Extensions.Caching.Memory;
using MiniMultiTenantPolicyApi.Data;
using MiniMultiTenantPolicyApi.Services;

namespace MiniMultiTenantPolicyApi.Tests;

public class CachedTenantCatalogTests
{
    [Fact]
    public async Task TenantExists_ReturnsTrueForSeededTenant()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var catalog = CreateCatalog(db);

        var exists = await catalog.TenantExistsAsync(1, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task TenantExists_ReturnsFalseForUnknownTenant()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var catalog = CreateCatalog(db);

        var exists = await catalog.TenantExistsAsync(999, CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task TenantExists_ServesSubsequentLookupsFromCache()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var catalog = CreateCatalog(db);

        Assert.True(await catalog.TenantExistsAsync(1, CancellationToken.None));

        await db.Tenants.ExecuteDeleteAsync();

        Assert.True(await catalog.TenantExistsAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task TenantExists_ReQueriesDatabaseAfterCacheExpiry()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var catalog = new CachedTenantCatalog(db, cache, TimeSpan.FromMilliseconds(1));

        Assert.True(await catalog.TenantExistsAsync(1, CancellationToken.None));

        await Task.Delay(20);
        await db.Tenants.ExecuteDeleteAsync();

        Assert.False(await catalog.TenantExistsAsync(1, CancellationToken.None));
    }

    private static CachedTenantCatalog CreateCatalog(AppDbContext db)
    {
        return new CachedTenantCatalog(db, new MemoryCache(new MemoryCacheOptions()));
    }
}
