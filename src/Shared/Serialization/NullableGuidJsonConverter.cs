using System.Text.Json;
using System.Text.Json.Serialization;

namespace AquaGas.Shared.Serialization;

public sealed class NullableGuidJsonConverter : JsonConverter<Guid?>
{
    public override Guid? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    return null;
                if (s.Equals("null", StringComparison.OrdinalIgnoreCase))
                    return null;
                if (!Guid.TryParse(s, out var id))
                    throw new JsonException($"Invalid GUID format: '{s}'.");
                return id;

            default:
                throw new JsonException(
                    "Optional customer id must be a JSON string (UUID), null, or omitted.");
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        Guid? value,
        JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value);
    }
}
