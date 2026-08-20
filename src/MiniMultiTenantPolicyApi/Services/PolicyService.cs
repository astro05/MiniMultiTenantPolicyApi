using Microsoft.EntityFrameworkCore;
using MiniMultiTenantPolicyApi.Data;
using MiniMultiTenantPolicyApi.DTOs;

namespace MiniMultiTenantPolicyApi.Services;

public class PolicyService : IPolicyService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IClock _clock;

    public PolicyService(AppDbContext db, ITenantContext tenantContext, IClock clock)
    {
        _db = db;
        _tenantContext = tenantContext;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ExpiringPolicyResponseDto>> GetExpiringPoliciesAsync(
        int withinDays,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var today = _clock.UtcNow.Date;
        var endDate = today.AddDays(withinDays);

        var policies = await _db.Policies
            .AsNoTracking()
            .Where(p =>
                p.Customer.TenantId == tenantId &&
                p.ExpirationDate >= today &&
                p.ExpirationDate <= endDate)
            .OrderBy(p => p.ExpirationDate)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

        return policies
            .Select(p => new ExpiringPolicyResponseDto
            {
                PolicyId = p.Id,
                CustomerId = p.CustomerId,
                PolicyNumber = p.PolicyNumber,
                ExpirationDate = DateOnly.FromDateTime(p.ExpirationDate),
                PremiumAmount = p.PremiumAmount
            })
            .ToList();
    }

    public async Task<PolicyExpirationResponseDto?> GetPolicyExpirationAsync(
        int policyId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var policy = await _db.Policies
            .AsNoTracking()
            .Where(p =>
                p.Id == policyId &&
                p.Customer.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            return null;
        }

        return new PolicyExpirationResponseDto
        {
            PolicyId = policy.Id,
            PolicyNumber = policy.PolicyNumber,
            ExpirationDate = DateOnly.FromDateTime(policy.ExpirationDate)
        };
    }
}
