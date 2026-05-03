using Xunit;
using AquaGas.Api.Modules.Auth.Domain.Factories;
using AquaGas.Api.Modules.Customer.Domain.Enums;

public class TypeDocumentFactoryTests
{
    [Theory]
    [InlineData("PF", TypeDocument.PF)]
    [InlineData("pf", TypeDocument.PF)]
    [InlineData(" PJ ", TypeDocument.PJ)]
    [InlineData("pj", TypeDocument.PJ)]
    public void Should_Return_Success_When_Value_Is_Valid(string input, TypeDocument expected)
    {
        var result = TypeDocumentFactory.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("123")]
    [InlineData("P F")]
    public void Should_Return_Failure_When_Value_Is_Invalid(string input)
    {
        var result = TypeDocumentFactory.Create(input);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.Equal("Type of document is invalid. Allowed: PF, PJ", result.Errors.FirstOrDefault()!.Message);
        Assert.Equal("TypeDocument", result.Errors.FirstOrDefault()!.Field);
    }

    [Fact]
    public void Should_Return_Failure_When_Value_Is_Null()
    {
        var result = TypeDocumentFactory.Create(null);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.Equal("TypeDocument is required", result.Errors.FirstOrDefault()!.Message);
        Assert.Equal("TypeDocument", result.Errors.FirstOrDefault()!.Field);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Should_Return_Failure_When_Value_Is_Empty_Or_Whitespace(string input)
    {
        var result = TypeDocumentFactory.Create(input);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.Equal("TypeDocument is required", result.Errors.FirstOrDefault()!.Message);
        Assert.Equal("TypeDocument", result.Errors.FirstOrDefault()!.Field);
    }
}