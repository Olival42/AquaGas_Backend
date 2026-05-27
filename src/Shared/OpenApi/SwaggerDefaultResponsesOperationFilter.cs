using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using AquaGas.Shared.Responses;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Nodes;

namespace AquaGas.Shared.OpenApi;

/// <summary>
/// Complementa respostas HTTP e força o schema de erro padronizado para códigos >= 400.
/// Adiciona 500 automaticamente para todos os endpoints.
/// </summary>
public sealed class SwaggerDefaultResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodAttributes = context.MethodInfo.GetCustomAttributes(inherit: true);
        var typeAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true) ?? [];

        var allAttributes = typeAttributes.Concat(methodAttributes).ToArray();

        var allowsAnonymous = allAttributes.OfType<AllowAnonymousAttribute>().Any();
        var authorizeAttributes = allAttributes.OfType<AuthorizeAttribute>().ToArray();
        var requiresAuth = authorizeAttributes.Length > 0 && !allowsAnonymous;
        var requiresRole = authorizeAttributes.Any(a => !string.IsNullOrWhiteSpace(a.Roles));

        var hasBody = context.ApiDescription.ParameterDescriptions
            .Any(p => p.Source == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Body);

        var httpMethod = context.ApiDescription.HttpMethod?.ToUpperInvariant();

        if (hasBody && httpMethod is "POST" or "PUT" or "PATCH")
        {
            AddResponseIfMissing(operation, "400", ApiDocumentation.Responses.BadRequestDescription);
        }

        if (requiresAuth)
        {
            AddResponseIfMissing(operation, "401", ApiDocumentation.Responses.UnauthorizedDescription);
        }

        if (requiresRole)
        {
            AddResponseIfMissing(operation, "403", ApiDocumentation.Responses.ForbiddenDescription);
        }

        AddResponseIfMissing(operation, "500", ApiDocumentation.Responses.InternalErrorDescription);

        var errorSchema = context.SchemaGenerator.GenerateSchema(typeof(ApiResponse<object>), context.SchemaRepository);

        if (operation.Responses != null)
        {
            foreach (var key in operation.Responses.Keys.ToList())
            {
                if (int.TryParse(key, out var statusCode) && statusCode >= 400)
                {
                    if (operation.Responses.TryGetValue(key, out var response) && response != null)
                    {
                        var (errorCode, errorMessage) = MapStatusCodeToError(statusCode);

                        var newResponse = new OpenApiResponse
                        {
                            Description = response.Description
                        };

                        newResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
                        newResponse.Links ??= new Dictionary<string, IOpenApiLink>();
                        newResponse.Content ??= new Dictionary<string, OpenApiMediaType>();

                        if (response.Headers is { Count: > 0 })
                        {
                            foreach (var header in response.Headers)
                            {
                                newResponse.Headers[header.Key] = header.Value;
                            }
                        }

                        if (response.Links is { Count: > 0 })
                        {
                            foreach (var link in response.Links)
                            {
                                newResponse.Links[link.Key] = link.Value;
                            }
                        }

                        newResponse.Content["application/json"] = new OpenApiMediaType
                        {
                            Schema = errorSchema,
                            Example = GetErrorExample(errorCode, errorMessage)
                        };

                        operation.Responses[key] = newResponse;
                    }
                }
            }
        }
    }

    private static void AddResponseIfMissing(OpenApiOperation operation, string statusCode, string description)
    {
        operation.Responses ??= new OpenApiResponses();

        if (operation.Responses.ContainsKey(statusCode))
            return;

        operation.Responses[statusCode] = new OpenApiResponse
        {
            Description = description
        };
    }

    private static (string Code, string Message) MapStatusCodeToError(int statusCode)
    {
        return statusCode switch
        {
            400 => ("VALIDATION_ERROR", "Validation failed"),
            401 => ("UNAUTHORIZED", "Unauthorized access"),
            403 => ("FORBIDDEN", "Forbidden access"),
            404 => ("NOT_FOUND", "Resource not found"),
            409 => ("CONFLICT", "Business rule conflict"),
            _ => ("INTERNAL_ERROR", "Something went wrong")
        };
    }

    private static JsonNode GetErrorExample(string errorCode, string message)
    {
        var jsonStr = $$"""
        {
          "success": false,
          "data": null,
          "error": {
            "code": "{{errorCode}}",
            "message": "{{message}}",
            "details": null
          },
          "timestamp": "2026-05-25T22:00:00Z"
        }
        """;
        return JsonNode.Parse(jsonStr)!;
    }
}