using Microsoft.EntityFrameworkCore;
using MiniMultiTenantPolicyApi.Data;
using MiniMultiTenantPolicyApi.DTOs;

namespace MiniMultiTenantPolicyApi.Services;

public class CustomerPolicyService : ICustomerPolicyService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CustomerPolicyService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PolicyResponseDto?> GetCustomerPolicyAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var policy = await _db.Policies
            .AsNoTracking()
            .Where(p =>
                p.CustomerId == customerId &&
                p.Customer.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            return null;
        }

        return new PolicyResponseDto
        {
            CustomerId = customerId,
            PolicyId = policy.Id,
            PolicyNumber = policy.PolicyNumber,
            ExpirationDate = DateOnly.FromDateTime(policy.ExpirationDate),
            PremiumAmount = policy.PremiumAmount
        };
    }
}
