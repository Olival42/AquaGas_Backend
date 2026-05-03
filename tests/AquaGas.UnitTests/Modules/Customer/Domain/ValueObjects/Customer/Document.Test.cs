using Xunit;
using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Customer;
using AquaGas.Api.Modules.Customer.Domain.Enums;

public class DocumentTests
{
    [Fact]
    public void Should_Create_CPF_When_Valid()
    {
        var input = "529.982.247-25";

        var result = Document.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(TypeDocument.PF, result.Value!.Type);
        Assert.Equal("52998224725", result.Value.Value);
    }

    [Fact]
    public void Should_Fail_When_CPF_Is_Invalid()
    {
        var input = "111.111.111-11";

        var result = Document.Create(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("CPF"));
    }

    [Fact]
    public void Should_Create_CNPJ_When_Valid()
    {
        var input = "04.252.011/0001-10";

        var result = Document.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(TypeDocument.PJ, result.Value!.Type);
        Assert.Equal("04252011000110", result.Value.Value);
    }

    [Fact]
    public void Should_Fail_When_CNPJ_Is_Invalid()
    {
        var input = "11.111.111/1111-11";

        var result = Document.Create(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("CNPJ"));
    }

    [Fact]
    public void Should_Fail_When_Length_Is_Invalid()
    {
        var input = "123456";

        var result = Document.Create(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message == "Invalid document");
    }

    [Fact]
    public void Should_Normalize_Input_Removing_Non_Digits()
    {
        var input = "529.982.247-25";

        var result = Document.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("52998224725", result.Value!.Value);
    }
}