using Xunit;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class PasswordTests
{
    [Fact]
    public void Should_Create_Valid_Password()
    {
        var result = Password.Create("Abc@1234");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Empty()
    {
        var result = Password.Create("");

        Assert.False(result.IsSuccess);
        Assert.Equal("Password cannot empty", result.Errors![0].Message);
    }

    [Fact]
    public void Should_Fail_When_Weak_Password()
    {
        var result = Password.Create("12345678");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Require_All_Complexity_Rules()
    {
        var result = Password.Create("abcdefg1");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Create_From_Hash()
    {
        var password = Password.FromHash("hashed_value");

        Assert.Equal("hashed_value", password.Value);
    }

    [Fact]
    public void Should_Fail_When_Hash_Is_Empty()
    {
        Assert.Throws<ArgumentException>(() =>
            Password.FromHash("")
        );
    }

    [Fact]
    public void Should_Convert_To_String()
    {
        var result = Password.Create("Abc@1234").Value!;

        string value = result;

        Assert.Equal(result.Value, value);
    }
}