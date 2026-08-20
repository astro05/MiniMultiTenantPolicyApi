namespace MiniMultiTenantPolicyApi.DTOs;

public class PolicyExpirationResponseDto
{
    public int PolicyId { get; set; }

    public string PolicyNumber { get; set; } = string.Empty;

    public DateOnly ExpirationDate { get; set; }
}
