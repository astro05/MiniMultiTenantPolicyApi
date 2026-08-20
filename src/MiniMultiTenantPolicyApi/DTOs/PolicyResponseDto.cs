namespace MiniMultiTenantPolicyApi.DTOs;

public class PolicyResponseDto
{
    public int CustomerId { get; set; }

    public int PolicyId { get; set; }

    public string PolicyNumber { get; set; } = string.Empty;

    public DateOnly ExpirationDate { get; set; }

    public decimal PremiumAmount { get; set; }
}
