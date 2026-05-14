using System.Text.Json;
using AquaGas.Shared.Serialization;
using Xunit;

public class NullableGuidJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public NullableGuidJsonConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new NullableGuidJsonConverter());
    }

    [Fact]
    public void Should_Deserialize_Valid_Guid()
    {
        var guid = Guid.NewGuid();
        var json = $"\"{guid}\"";

        var result = JsonSerializer.Deserialize<Guid?>(json, _options);

        Assert.Equal(guid, result);
    }

    [Fact]
    public void Should_Deserialize_Null()
    {
        var result = JsonSerializer.Deserialize<Guid?>(
            "null",
            _options);

        Assert.Null(result);
    }

    [Fact]
    public void Should_Deserialize_Empty_String_As_Null()
    {
        var result = JsonSerializer.Deserialize<Guid?>(
            "\"\"",
            _options);

        Assert.Null(result);
    }

    [Fact]
    public void Should_Deserialize_Whitespace_String_As_Null()
    {
        var result = JsonSerializer.Deserialize<Guid?>(
            "\"   \"",
            _options);

        Assert.Null(result);
    }

    [Fact]
    public void Should_Deserialize_String_Null_As_Null()
    {
        var result = JsonSerializer.Deserialize<Guid?>(
            "\"null\"",
            _options);

        Assert.Null(result);
    }

    [Fact]
    public void Should_Deserialize_String_Null_Ignoring_Case_As_Null()
    {
        var result = JsonSerializer.Deserialize<Guid?>(
            "\"NULL\"",
            _options);

        Assert.Null(result);
    }

    [Fact]
    public void Should_Throw_When_Guid_Is_Invalid()
    {
        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Guid?>(
                "\"invalid-guid\"",
                _options));

        Assert.Equal(
            "Invalid GUID format: 'invalid-guid'.",
            exception.Message);
    }

    [Fact]
    public void Should_Throw_When_Token_Is_Not_String_Or_Null()
    {
        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Guid?>(
                "123",
                _options));

        Assert.Equal(
            "Optional customer id must be a JSON string (UUID), null, or omitted.",
            exception.Message);
    }

    [Fact]
    public void Should_Serialize_Guid()
    {
        var guid = Guid.NewGuid();

        var json = JsonSerializer.Serialize<Guid?>(
            guid,
            _options);

        Assert.Equal($"\"{guid}\"", json);
    }

    [Fact]
    public void Should_Serialize_Null()
    {
        var json = JsonSerializer.Serialize<Guid?>(
            null,
            _options);

        Assert.Equal("null", json);
    }
}