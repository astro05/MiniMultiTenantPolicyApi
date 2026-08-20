namespace MiniMultiTenantPolicyApi.Services;

public class TenantHeaderException : Exception
{
    public TenantHeaderException(string message)
        : base(message)
    {
    }
}
