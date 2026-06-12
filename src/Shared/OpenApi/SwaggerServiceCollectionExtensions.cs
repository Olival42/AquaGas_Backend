using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AquaGas.Shared.OpenApi;

public static class SwaggerServiceCollectionExtensions
{
    public static IServiceCollection AddAquaGasSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(ApiDocumentation.Version, new OpenApiInfo
            {
                Title = ApiDocumentation.Title,
                Version = ApiDocumentation.Version,
                Description = ApiDocumentation.Description,
            });

            options.AddSecurityDefinition(ApiDocumentation.Security.BearerSchemeId, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = ApiDocumentation.Security.BearerDescription,
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    ApiDocumentation.Security.BearerSchemeId,
                    document)] = []
            });

            options.TagActionsBy(api =>
            {
                var controllerTag = api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller)
                    ? MapControllerTag(controller)
                    : ApiDocumentation.Tags.Auth;

                return [controllerTag];
            });

            options.OrderActionsBy(apiDesc =>
                $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.RelativePath}");

            options.EnableAnnotations();
            options.SupportNonNullableReferenceTypes();
            options.DescribeAllParametersInCamelCase();

            options.OperationFilter<SwaggerDefaultResponsesOperationFilter>();
            options.SchemaFilter<SwaggerSchemaDocumentationFilter>();

            options.MapType<DateTime>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "date-time",
                Description = "Data/hora ISO 8601 (UTC recomendado). Exemplo: `2026-01-01T00:00:00Z`."
            });

            options.MapType<DateTimeOffset>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "date-time",
                Description = "Data/hora ISO 8601 com offset. Exemplo: `2026-01-01T00:00:00+00:00`."
            });

            IncludeXmlComments(options);
        });

        return services;
    }

    private static string MapControllerTag(string? controller) =>
        controller?.ToLowerInvariant() switch
        {
            "auth" => ApiDocumentation.Tags.Auth,
            "customer" => ApiDocumentation.Tags.Customers,
            "employee" => ApiDocumentation.Tags.Employees,
            "product" => ApiDocumentation.Tags.Products,
            "sale" => ApiDocumentation.Tags.Sales,
            "plan" => ApiDocumentation.Tags.Plans,
            "penalty" => ApiDocumentation.Tags.Penalties,
            "report" => ApiDocumentation.Tags.Reports,
            _ => controller ?? "API"
        };

    private static void IncludeXmlComments(SwaggerGenOptions options)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var xmlFiles = Directory.GetFiles(baseDirectory, "*.xml", SearchOption.TopDirectoryOnly);

        foreach (var xmlFile in xmlFiles)
        {
            options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
        }

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null)
            return;

        var referencedAssemblies = entryAssembly
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .Where(a => a.GetName().Name?.StartsWith("AquaGas", StringComparison.OrdinalIgnoreCase) == true);

        foreach (var assembly in referencedAssemblies)
        {
            var xmlPath = Path.Combine(baseDirectory, $"{assembly.GetName().Name}.xml");
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }
}
