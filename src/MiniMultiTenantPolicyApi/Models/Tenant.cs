namespace MiniMultiTenantPolicyApi.Models;

public class Tenant
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
