using Microsoft.AspNetCore.Mvc;
using MiniMultiTenantPolicyApi.DTOs;
using MiniMultiTenantPolicyApi.Services;

namespace MiniMultiTenantPolicyApi.Controllers;

[ApiController]
[Route("api/policies")]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public PoliciesController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpGet("expiring")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpiringPolicyResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ExpiringPolicyResponseDto>>> GetExpiringPolicies(
        [FromQuery] int? withinDays,
        CancellationToken cancellationToken)
    {
        var days = withinDays ?? 30;

        if (days < 0)
        {
            return BadRequest(new { error = "withinDays must be greater than or equal to 0." });
        }

        var policies = await _policyService.GetExpiringPoliciesAsync(days, cancellationToken);

        return Ok(policies);
    }

    [HttpGet("{policyId:int:min(1)}/expiration")]
    [ProducesResponseType(typeof(PolicyExpirationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PolicyExpirationResponseDto>> GetPolicyExpiration(
        int policyId,
        CancellationToken cancellationToken)
    {
        var policy = await _policyService.GetPolicyExpirationAsync(policyId, cancellationToken);

        if (policy is null)
        {
            return NotFound();
        }

        return Ok(policy);
    }
}
