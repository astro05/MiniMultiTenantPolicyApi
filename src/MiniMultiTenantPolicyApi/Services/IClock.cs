namespace MiniMultiTenantPolicyApi.Services;

public interface IClock
{
    DateTime UtcNow { get; }
}
