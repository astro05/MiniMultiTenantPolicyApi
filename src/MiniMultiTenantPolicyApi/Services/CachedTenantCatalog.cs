using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiniMultiTenantPolicyApi.Data;

namespace MiniMultiTenantPolicyApi.Services;

public class CachedTenantCatalog : ITenantCatalog
{
    private const string CacheKey = "TenantCatalog.Ids";

    private static readonly TimeSpan DefaultCacheLifetime = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheLifetime;

    public CachedTenantCatalog(AppDbContext db, IMemoryCache cache)
        : this(db, cache, DefaultCacheLifetime)
    {
    }

    public CachedTenantCatalog(AppDbContext db, IMemoryCache cache, TimeSpan cacheLifetime)
    {
        _db = db;
        _cache = cache;
        _cacheLifetime = cacheLifetime;
    }

    public async Task<bool> TenantExistsAsync(int tenantId, CancellationToken cancellationToken)
    {
        var tenantIds = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheLifetime;

            return await _db.Tenants
                .AsNoTracking()
                .Select(t => t.Id)
                .ToHashSetAsync(cancellationToken);
        });

        return tenantIds?.Contains(tenantId) ?? false;
    }
}
