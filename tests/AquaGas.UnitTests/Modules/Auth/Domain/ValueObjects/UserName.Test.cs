using Xunit;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class UserNameTests
{
    [Fact]
    public void Should_Create_Valid_UserName()
    {
        var result = UserName.Create("John123");

        Assert.True(result.IsSuccess);
        Assert.Equal("john123", result.Value!.Value);
    }

    [Fact]
    public void Should_Convert_To_LowerCase()
    {
        var result = UserName.Create("JohnDoe");

        Assert.Equal("johndoe", result.Value!.Value);
    }

    [Fact]
    public void Should_Fail_When_Empty()
    {
        var result = UserName.Create("");

        Assert.False(result.IsSuccess);
        Assert.Equal("Username cannot be empty.", result.Errors![0].Message);
    }

    [Fact]
    public void Should_Fail_When_Invalid_Characters()
    {
        var result = UserName.Create("john@123");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Too_Short()
    {
        var result = UserName.Create("ab");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Too_Long()
    {
        var longName = new string('a', 51);

        var result = UserName.Create(longName);

        Assert.False(result.IsSuccess);
    }
}