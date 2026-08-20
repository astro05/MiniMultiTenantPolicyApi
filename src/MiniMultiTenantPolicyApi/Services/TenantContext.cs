namespace MiniMultiTenantPolicyApi.Services;

public class TenantContext : ITenantContext
{
    public const string HeaderName = "X-Tenant-Id";

    public int TenantId { get; }

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(header))
        {
            throw new TenantHeaderException("X-Tenant-Id header is required.");
        }

        if (!int.TryParse(header, out var tenantId) || tenantId <= 0)
        {
            throw new TenantHeaderException("X-Tenant-Id header must be a positive integer.");
        }

        TenantId = tenantId;
    }
}
