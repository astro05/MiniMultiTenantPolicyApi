using MiniMultiTenantPolicyApi.Data;
using MiniMultiTenantPolicyApi.Services;

namespace MiniMultiTenantPolicyApi.Tests;

internal static class TestDb
{
    public static AppDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static SqliteConnection OpenInMemoryConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }
}

internal sealed class FixedClock : IClock
{
    public FixedClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; }
}

internal sealed class FakeTenantContext : ITenantContext
{
    public FakeTenantContext(int tenantId)
    {
        TenantId = tenantId;
    }

    public int TenantId { get; }
}

internal static class TestDates
{
    public static readonly DateTime Today = new(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc);

    public static FixedClock Clock => new(Today);
}
