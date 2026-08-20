using MiniMultiTenantPolicyApi.DTOs;

namespace MiniMultiTenantPolicyApi.Services;

public interface ICustomerPolicyService
{
    Task<PolicyResponseDto?> GetCustomerPolicyAsync(
        int customerId,
        CancellationToken cancellationToken);
}
