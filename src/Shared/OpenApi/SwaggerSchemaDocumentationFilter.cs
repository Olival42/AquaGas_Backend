using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AquaGas.Shared.OpenApi;

/// <summary>
/// Enriquece schemas com formatos válidos (CPF, CNPJ, telefone, enums), patterns e exemplos de requisição.
/// </summary>
public sealed class SwaggerSchemaDocumentationFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema openApiSchema)
            return;

        if (context.MemberInfo is PropertyInfo property)
        {
            var rule = ApiFieldFormats.Resolve(property, context.Type);
            ApplyPropertyRule(openApiSchema, rule);

            if (IsEnumOrNullableEnum(property.PropertyType, out var enumType))
            {
                var names = Enum.GetNames(enumType);
                openApiSchema.Description = MergeDescription(
                    openApiSchema.Description,
                    $"Valores válidos: `{string.Join("`, `", names)}`.");

                if (openApiSchema.Enum is null || openApiSchema.Enum.Count == 0)
                {
                    openApiSchema.Enum = names.Select(name => (JsonNode)JsonValue.Create(name)!).ToList();
                }

                if (openApiSchema.Example is null && string.IsNullOrWhiteSpace(rule.Example))
                {
                    openApiSchema.Example = JsonValue.Create(names.FirstOrDefault() ?? "");
                }
            }
            return;
        }

        if (context.Type != null && IsEnumOrNullableEnum(context.Type, out var underlyingEnum))
        {
            ApplyEnumRule(openApiSchema, underlyingEnum);
            return;
        }

        var typeRule = ApiFieldFormats.ResolveType(context.Type!.Name);
        if (typeRule is not null)
            ApplyTypeRule(openApiSchema, typeRule);
    }

    private static bool IsEnumOrNullableEnum(Type type, out Type underlyingEnumType)
    {
        underlyingEnumType = null!;
        if (type.IsEnum)
        {
            underlyingEnumType = type;
            return true;
        }
        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying != null && nullableUnderlying.IsEnum)
        {
            underlyingEnumType = nullableUnderlying;
            return true;
        }
        return false;
    }

    private static void ApplyPropertyRule(OpenApiSchema schema, ApiFieldFormats.FieldRule rule)
    {
        if (rule == ApiFieldFormats.Empty)
            return;

        var descriptionParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(rule.Description))
            descriptionParts.Add(rule.Description.Trim());

        if (!string.IsNullOrWhiteSpace(rule.Example))
            descriptionParts.Add($"Exemplo: `{rule.Example}`.");

        if (rule.AllowedValues is { Length: > 0 })
            descriptionParts.Add($"Valores válidos: `{string.Join("`, `", rule.AllowedValues)}`.");

        if (descriptionParts.Count > 0)
            schema.Description = MergeDescription(schema.Description, string.Join(" ", descriptionParts));

        if (!string.IsNullOrWhiteSpace(rule.Pattern))
            schema.Pattern = rule.Pattern;

        if (!string.IsNullOrWhiteSpace(rule.Format))
            schema.Format = rule.Format;

        if (rule.Minimum.HasValue)
            schema.Minimum = rule.Minimum.Value.ToString(CultureInfo.InvariantCulture);

        if (rule.Maximum.HasValue)
            schema.Maximum = rule.Maximum.Value.ToString(CultureInfo.InvariantCulture);

        if (rule.MinLength.HasValue)
            schema.MinLength = rule.MinLength;

        if (rule.MaxLength.HasValue)
            schema.MaxLength = rule.MaxLength;

        if (!string.IsNullOrWhiteSpace(rule.Example))
        {
            SetSchemaExample(schema, rule.Example);
        }
        else if (rule.AllowedValues is { Length: > 0 })
        {
            schema.Example = JsonValue.Create(rule.AllowedValues[0]);
        }
    }

    private static void SetSchemaExample(OpenApiSchema schema, string example)
    {
        if (string.IsNullOrWhiteSpace(example))
            return;

        if (schema.Type == JsonSchemaType.Integer)
        {
            if (int.TryParse(example, out var intVal))
            {
                schema.Example = JsonValue.Create(intVal);
                return;
            }
        }
        else if (schema.Type == JsonSchemaType.Number)
        {
            if (double.TryParse(example, CultureInfo.InvariantCulture, out var doubleVal))
            {
                schema.Example = JsonValue.Create(doubleVal);
                return;
            }
        }
        else if (schema.Type == JsonSchemaType.Boolean)
        {
            if (bool.TryParse(example, out var boolVal))
            {
                schema.Example = JsonValue.Create(boolVal);
                return;
            }
        }

        schema.Example = JsonValue.Create(example);
    }

    private static void ApplyEnumRule(OpenApiSchema schema, Type enumType)
    {
        var names = Enum.GetNames(enumType);
        schema.Description = MergeDescription(
            schema.Description,
            $"Valores válidos: `{string.Join("`, `", names)}`.");
    }

    private static void ApplyTypeRule(OpenApiSchema schema, ApiFieldFormats.FieldRule rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.Description))
            schema.Description = MergeDescription(schema.Description, rule.Description);

        if (string.IsNullOrWhiteSpace(rule.TypeExampleJson))
            return;

        var exampleBlock = new StringBuilder()
            .AppendLine()
            .AppendLine("**Exemplo de requisição:**")
            .AppendLine("```json")
            .AppendLine(rule.TypeExampleJson.Trim())
            .AppendLine("```")
            .ToString();

        schema.Description = MergeDescription(schema.Description, exampleBlock);

        try
        {
            schema.Example = JsonNode.Parse(rule.TypeExampleJson);
        }
        catch
        {
            // Ignore parse errors to avoid breaking Swagger generation
        }
    }

    private static string? MergeDescription(string? current, string? addition)
    {
        if (string.IsNullOrWhiteSpace(addition))
            return current;

        if (string.IsNullOrWhiteSpace(current))
            return addition.Trim();

        if (current.Contains(addition, StringComparison.Ordinal))
            return current;

        return $"{current.TrimEnd()}{addition}";
    }
}
