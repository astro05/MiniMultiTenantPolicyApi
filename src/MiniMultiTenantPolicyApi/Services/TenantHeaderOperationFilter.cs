using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MiniMultiTenantPolicyApi.Services;

public class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = TenantContext.HeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Tenant identifier. Demo tenants: 1 = Acme Insurance, 2 = Globex Insurance.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer }
        });
    }
}
