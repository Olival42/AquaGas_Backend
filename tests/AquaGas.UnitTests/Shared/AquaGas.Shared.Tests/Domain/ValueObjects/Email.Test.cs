using Xunit;
using AquaGas.Shared.Domain.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Should_Create_Valid_Email()
    {
        var result = Email.Create("test@email.com");

        Assert.True(result.IsSuccess);
        Assert.Equal("test@email.com", result.Value!.Value);
    }

    [Fact]
    public void Should_Convert_To_Lowercase()
    {
        var result = Email.Create("TEST@EMAIL.COM");

        Assert.True(result.IsSuccess);
        Assert.Equal("test@email.com", result.Value!.Value);
    }

    [Fact]
    public void Should_Fail_When_Empty()
    {
        var result = Email.Create("");

        Assert.False(result.IsSuccess);
        Assert.Equal("Email cannot be empty", result.Errors![0].Message);
    }

    [Fact]
    public void Should_Fail_When_Null()
    {
        var result = Email.Create(null!);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Invalid_Format()
    {
        var result = Email.Create("invalid-email");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email", result.Errors![0].Message);
    }

    [Fact]
    public void Should_Fail_When_Missing_Domain()
    {
        var result = Email.Create("test@");

        Assert.False(result.IsSuccess);
    }
}