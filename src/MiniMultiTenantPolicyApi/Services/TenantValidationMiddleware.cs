namespace MiniMultiTenantPolicyApi.Services;

public class TenantValidationMiddleware
{
    private readonly RequestDelegate _next;

    public TenantValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantCatalog tenantCatalog)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();

        var tenantId = tenantContext.TenantId;

        var tenantExists = await tenantCatalog.TenantExistsAsync(tenantId, context.RequestAborted);

        if (!tenantExists)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, $"Tenant '{tenantId}' does not exist.");
            return;
        }

        await _next(context);
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
