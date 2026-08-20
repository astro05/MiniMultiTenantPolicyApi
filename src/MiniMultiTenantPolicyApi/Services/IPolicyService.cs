using MiniMultiTenantPolicyApi.DTOs;

namespace MiniMultiTenantPolicyApi.Services;

public interface IPolicyService
{
    Task<IReadOnlyList<ExpiringPolicyResponseDto>> GetExpiringPoliciesAsync(
        int withinDays,
        CancellationToken cancellationToken);

    Task<PolicyExpirationResponseDto?> GetPolicyExpirationAsync(
        int policyId,
        CancellationToken cancellationToken);
}
