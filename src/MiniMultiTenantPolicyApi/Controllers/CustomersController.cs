using Microsoft.AspNetCore.Mvc;
using MiniMultiTenantPolicyApi.DTOs;
using MiniMultiTenantPolicyApi.Services;

namespace MiniMultiTenantPolicyApi.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerPolicyService _customerPolicyService;

    public CustomersController(ICustomerPolicyService customerPolicyService)
    {
        _customerPolicyService = customerPolicyService;
    }

    [HttpGet("{customerId:int:min(1)}/policy")]
    [ProducesResponseType(typeof(PolicyResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PolicyResponseDto>> GetCustomerPolicy(
        int customerId,
        CancellationToken cancellationToken)
    {
        var policy = await _customerPolicyService.GetCustomerPolicyAsync(customerId, cancellationToken);

        if (policy is null)
        {
            return NotFound();
        }

        return Ok(policy);
    }
}
