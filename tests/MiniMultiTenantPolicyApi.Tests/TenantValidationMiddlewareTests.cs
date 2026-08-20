using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MiniMultiTenantPolicyApi.Data;
using MiniMultiTenantPolicyApi.Services;

namespace MiniMultiTenantPolicyApi.Tests;

public class TenantValidationMiddlewareTests
{
    [Fact]
    public async Task Invoke_WhenHeaderMissing_ThrowsTenantHeaderException()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        var context = CreateHttpContext(headerValue: null);

        var nextInvoked = false;
        var middleware = new TenantValidationMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<TenantHeaderException>(() => middleware.InvokeAsync(context, db));

        Assert.False(nextInvoked);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-5")]
    public async Task Invoke_WhenHeaderInvalid_ThrowsTenantHeaderException(string headerValue)
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        var context = CreateHttpContext(headerValue);

        var nextInvoked = false;
        var middleware = new TenantValidationMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<TenantHeaderException>(() => middleware.InvokeAsync(context, db));

        Assert.False(nextInvoked);
    }

    [Fact]
    public async Task Invoke_WhenTenantDoesNotExist_Writes400()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var context = CreateHttpContext("999");
        context.Response.Body = new MemoryStream();

        var middleware = new TenantValidationMiddleware(_ => throw new InvalidOperationException("next must not be invoked"));

        await middleware.InvokeAsync(context, db);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Contains("does not exist", body);
    }

    [Fact]
    public async Task Invoke_WhenTenantExists_CallsNext()
    {
        using var connection = TestDb.OpenInMemoryConnection();
        using var db = TestDb.CreateContext(connection);
        await new DbSeeder(db, TestDates.Clock).SeedAsync();
        var context = CreateHttpContext("1");

        var nextInvoked = false;
        var middleware = new TenantValidationMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, db);

        Assert.True(nextInvoked);
    }

    private static HttpContext CreateHttpContext(string? headerValue)
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };
        httpContext.Request.Path = "/api/customers/101/policy";

        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        if (headerValue is not null)
        {
            httpContext.Request.Headers[TenantContext.HeaderName] = headerValue;
        }

        return httpContext;
    }
}
