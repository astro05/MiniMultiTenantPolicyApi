using Microsoft.EntityFrameworkCore;
using MiniMultiTenantPolicyApi.Data;

namespace MiniMultiTenantPolicyApi.Services;

public class TenantValidationMiddleware
{
    private readonly RequestDelegate _next;

    public TenantValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var header = context.Request.Headers[TenantContext.HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(header))
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "X-Tenant-Id header is required.");
            return;
        }

        if (!int.TryParse(header, out var tenantId) || tenantId <= 0)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "X-Tenant-Id header must be a positive integer.");
            return;
        }

        var tenantExists = await db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId, context.RequestAborted);

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
