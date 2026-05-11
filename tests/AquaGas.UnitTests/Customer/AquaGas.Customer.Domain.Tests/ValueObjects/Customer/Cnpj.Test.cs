using Xunit;
using AquaGas.Customer.Domain.ValueObjects.Customer;

public class CnpjTests
{
    [Fact]
    public void Should_Create_When_Cnpj_Is_Valid()
    {
        var input = "04.252.011/0001-10";

        var result = Cnpj.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("04252011000110", result.Value!.Value);
    }

    [Fact]
    public void Should_Normalize_Removing_Non_Digits()
    {
        var input = "04.252.011/0001-10";

        var result = Cnpj.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("04252011000110", result.Value!.Value);
    }

    [Fact]
    public void Should_Fail_When_Cnpj_Is_Null_Or_Empty()
    {
        var result1 = Cnpj.Create(null!);
        var result2 = Cnpj.Create("");
        var result3 = Cnpj.Create("   ");

        Assert.True(result1.IsFailure);
        Assert.True(result2.IsFailure);
        Assert.True(result3.IsFailure);
    }

    [Fact]
    public void Should_Fail_When_All_Digits_Are_Equal()
    {
        var input = "11.111.111/1111-11";

        var result = Cnpj.Create(input);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Should_Fail_When_Cnpj_Is_Invalid()
    {
        var input = "04.252.011/0001-00";

        var result = Cnpj.Create(input);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("04252011000110")]
    [InlineData("04.252.011/0001-10")]
    public void Should_Accept_Valid_Formats(string input)
    {
        var result = Cnpj.Create(input);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Length_Is_Invalid()
    {
        var input = "123";

        var result = Cnpj.Create(input);

        Assert.True(result.IsFailure);
    }
}