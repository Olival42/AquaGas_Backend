using Xunit;
using AquaGas.Auth.Domain.Factories;
using AquaGas.Auth.Domain.Enums;

public class RoleFactoryTests
{
    [Theory]
    [InlineData("Manager", Role.Manager)]
    [InlineData("Employee", Role.Employee)]
    [InlineData("manager", Role.Manager)]
    [InlineData("employee", Role.Employee)]
    public void Should_Create_Role_When_Valid(string input, Role expected)
    {
        var result = RoleFactory.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    [InlineData("")]
    public void Should_Fail_When_Role_Invalid(string input)
    {
        var result = RoleFactory.Create(input);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Errors.First().Code);
    }

    [Fact]
    public void Should_Fail_When_Role_Null()
    {
        var result = RoleFactory.Create(null!);

        Assert.False(result.IsSuccess);
    }
}