using Microsoft.AspNetCore.Http;
using MiniMultiTenantPolicyApi.Services;

namespace MiniMultiTenantPolicyApi.Tests;

public class TenantContextTests
{
    [Fact]
    public void Constructor_WithValidHeader_ParsesTenantId()
    {
        var context = new TenantContext(CreateAccessor("42"));

        Assert.Equal(42, context.TenantId);
    }

    [Fact]
    public void Constructor_WithMissingHeader_ThrowsTenantHeaderException()
    {
        var exception = Assert.Throws<TenantHeaderException>(() => new TenantContext(CreateAccessor(null)));

        Assert.Equal("X-Tenant-Id header is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithNonNumericHeader_ThrowsTenantHeaderException()
    {
        Assert.Throws<TenantHeaderException>(() => new TenantContext(CreateAccessor("abc")));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("")]
    public void Constructor_WithNonPositiveHeader_ThrowsTenantHeaderException(string headerValue)
    {
        Assert.Throws<TenantHeaderException>(() => new TenantContext(CreateAccessor(headerValue)));
    }

    private static IHttpContextAccessor CreateAccessor(string? headerValue)
    {
        var httpContext = new DefaultHttpContext();

        if (headerValue is not null)
        {
            httpContext.Request.Headers[TenantContext.HeaderName] = headerValue;
        }

        return new HttpContextAccessor { HttpContext = httpContext };
    }
}
