using Xunit;
using AquaGas.Shared.Domain.ValueObjects;

public class PhoneTests
{
    [Fact]
    public void Should_Create_Valid_Phone()
    {
        var result = Phone.Create("11999999999");

        Assert.True(result.IsSuccess);
        Assert.Equal("11999999999", result.Value!.Value);
    }

    [Fact]
    public void Should_Remove_Mask()
    {
        var result = Phone.Create("(11) 99999-9999");

        Assert.True(result.IsSuccess);
        Assert.Equal("11999999999", result.Value!.Value);
    }

    [Fact]
    public void Should_Fail_When_Empty()
    {
        var result = Phone.Create("");

        Assert.False(result.IsSuccess);
        Assert.Equal("Phone cannot be empty", result.Errors![0].Message);
    }

    [Fact]
    public void Should_Fail_When_Null()
    {
        var result = Phone.Create(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal("Phone cannot be empty", result.Errors![0].Message);
    }

    [Fact]
    public void Should_Fail_When_Invalid_Length()
    {
        var result = Phone.Create("123");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid phone", result.Errors![0].Message);
    }
}