namespace MiniMultiTenantPolicyApi.Services;

public interface ITenantCatalog
{
    Task<bool> TenantExistsAsync(int tenantId, CancellationToken cancellationToken);
}
