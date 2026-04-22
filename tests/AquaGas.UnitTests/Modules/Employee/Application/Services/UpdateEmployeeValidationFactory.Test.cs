using Xunit;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;

public class UpdateEmployeeValidationFactoryTests
{
    [Fact]
    public void Should_Return_Success_When_All_Null()
    {
        var input = new UpdateEmployeeInput();

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void Should_Return_Success_When_Name_Valid()
    {
        var input = new UpdateEmployeeInput
        {
            Name = "João Silva"
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Name);
    }

    [Fact]
    public void Should_Fail_When_Email_Invalid()
    {
        var input = new UpdateEmployeeInput
        {
            Email = "email-invalido"
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Multiple_Invalid()
    {
        var input = new UpdateEmployeeInput
        {
            Name = "",
            Email = "email-invalido",
            Phone = ""
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.False(result.IsSuccess);
        Assert.True(result.Errors.Count > 1);
    }

    [Fact]
    public void Should_Return_Success_When_Role_Valid()
    {
        var input = new UpdateEmployeeInput
        {
            Role = "Manager"
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Role);
    }

    [Fact]
    public void Should_Fail_When_Role_Invalid()
    {
        var input = new UpdateEmployeeInput
        {
            Role = "InvalidRole"
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Return_Success_When_Role_And_Name_Valid()
    {
        var input = new UpdateEmployeeInput
        {
            Name = "João",
            Role = "Employee"
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Role);
        Assert.NotNull(result.Value.Name);
    }

    [Fact]
    public void Should_Fail_When_Role_Invalid_And_Email_Invalid()
    {
        var input = new UpdateEmployeeInput
        {
            Role = "InvalidRole",
            Email = "email-invalido"
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.False(result.IsSuccess);
        Assert.True(result.Errors.Count >= 2);
    }

    [Fact]
    public void Should_Return_Success_When_Mix_Valid_And_Null()
    {
        var input = new UpdateEmployeeInput
        {
            Name = "João"
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Name);
        Assert.Null(result.Value.Email);
        Assert.Null(result.Value.Phone);
    }
}