using Xunit;
using AquaGas.Auth.Domain.ValueObjects;

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

    [Fact]
    public void Should_Be_Equal_When_Same_Value()
    {
        var a = UserName.Create("john123").Value!;
        var b = UserName.Create("john123").Value!;

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Should_Not_Be_Equal_When_Different_Value()
    {
        var a = UserName.Create("john123").Value!;
        var b = UserName.Create("maria123").Value!;

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Should_Not_Be_Equal_To_Null()
    {
        var a = UserName.Create("john123").Value!;

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void Should_Have_Same_HashCode_When_Same_Value()
    {
        var a = UserName.Create("john123").Value!;
        var b = UserName.Create("john123").Value!;

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}