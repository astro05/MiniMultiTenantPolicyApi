namespace MiniMultiTenantPolicyApi.Models;

public class Policy
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string PolicyNumber { get; set; } = string.Empty;

    public DateTime ExpirationDate { get; set; }

    public decimal PremiumAmount { get; set; }

    public Customer Customer { get; set; } = null!;
}
