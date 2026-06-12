using Xunit;
using AquaGas.Shared.Domain.ValueObjects;

public class CpfTests
{
    [Fact]
    public void Should_Create_Valid_Cpf()
    {
        var result = Cpf.Create("529.982.247-25");

        Assert.True(result.IsSuccess);
        Assert.Equal("52998224725", result.Value!.Value);
    }

    [Fact]
    public void Should_Remove_Mask()
    {
        var result = Cpf.Create("529.982.247-25");

        Assert.Equal("52998224725", result.Value!.Value);
    }

    [Fact]
    public void Should_Fail_When_Empty()
    {
        var result = Cpf.Create("");

        Assert.False(result.IsSuccess);
        Assert.Equal("CPF cannot be empty", result.Errors![0].Message);
    }

    [Fact]
    public void Should_Fail_When_Invalid_Cpf()
    {
        var result = Cpf.Create("12345678900");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid CPF", result.Errors![0].Message);
    }

    [Fact]
    public void Should_Fail_When_All_Digits_Same()
    {
        var result = Cpf.Create("11111111111");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Wrong_Length()
    {
        var result = Cpf.Create("123");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Accept_Unformatted_Cpf()
    {
        var result = Cpf.Create("52998224725");

        Assert.True(result.IsSuccess);
    }
}