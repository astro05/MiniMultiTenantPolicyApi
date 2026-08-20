namespace MiniMultiTenantPolicyApi.Models;

public class Customer
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Tenant Tenant { get; set; } = null!;

    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
