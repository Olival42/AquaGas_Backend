using Microsoft.AspNetCore.Builder;

namespace AquaGas.Shared.OpenApi;

public static class SwaggerApplicationBuilderExtensions
{
    public static WebApplication UseAquaGasSwagger(this WebApplication app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "openapi/{documentName}/openapi.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                $"/openapi/{ApiDocumentation.Version}/openapi.json",
                $"{ApiDocumentation.Title} {ApiDocumentation.Version}");

            options.RoutePrefix = "swagger";
            options.DocumentTitle = ApiDocumentation.Title;
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.DisplayOperationId();
        });

        return app;
    }
}
