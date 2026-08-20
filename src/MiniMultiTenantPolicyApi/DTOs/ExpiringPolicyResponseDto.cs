namespace MiniMultiTenantPolicyApi.DTOs;

public class ExpiringPolicyResponseDto
{
    public int PolicyId { get; set; }

    public int CustomerId { get; set; }

    public string PolicyNumber { get; set; } = string.Empty;

    public DateOnly ExpirationDate { get; set; }

    public decimal PremiumAmount { get; set; }
}
