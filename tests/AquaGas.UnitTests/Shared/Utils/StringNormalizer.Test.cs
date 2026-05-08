using AquaGas.Api.Shared.Utils;
using Xunit;

namespace AquaGas.Tests.Shared.Utils;

public class StringNormalizerTests
{
    [Theory]
    [InlineData("água mineral", "AGUA MINERAL")]
    [InlineData("ÁGUA MINERAL", "AGUA MINERAL")]
    [InlineData("AgUa MiNeRaL", "AGUA MINERAL")]
    [InlineData(" gás ", "GAS")]
    [InlineData("çãéíõ", "CAEIO")]
    [InlineData("João", "JOAO")]
    [InlineData("José da Silva", "JOSE DA SILVA")]
    public void Should_Normalize_String_Correctly(
        string input,
        string expected)
    {
        var result = StringNormalizer.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Should_Return_Empty_When_Input_Is_Whitespace(
        string input)
    {
        var result = StringNormalizer.Normalize(input);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Should_Return_Empty_When_Input_Is_Null()
    {
        var result = StringNormalizer.Normalize(null!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Should_Remove_Accents()
    {
        var result = StringNormalizer.Normalize("áàâãäéèêë");

        Assert.Equal("AAAAAEEEE", result);
    }

    [Fact]
    public void Should_Keep_Numbers()
    {
        var result = StringNormalizer.Normalize("Água 20L");

        Assert.Equal("AGUA 20L", result);
    }

    [Fact]
    public void Should_Keep_Special_Characters()
    {
        var result = StringNormalizer.Normalize("Água/Gás");

        Assert.Equal("AGUA/GAS", result);
    }

    [Fact]
    public void Should_Not_Change_Already_Normalized_String()
    {
        var result = StringNormalizer.Normalize("AGUA MINERAL");

        Assert.Equal("AGUA MINERAL", result);
    }

    [Fact]
    public void Should_Be_Case_Insensitive()
    {
        var first = StringNormalizer.Normalize("agua");
        var second = StringNormalizer.Normalize("AGUA");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Should_Remove_Trailing_And_Leading_Spaces()
    {
        var result = StringNormalizer.Normalize("   Água Mineral   ");

        Assert.Equal("AGUA MINERAL", result);
    }

    [Fact]
    public void Should_Handle_Multiple_Spaces_Between_Words()
    {
        var result = StringNormalizer.Normalize("Agua    Mineral");

        Assert.Equal("AGUA MINERAL", result);
    }
}