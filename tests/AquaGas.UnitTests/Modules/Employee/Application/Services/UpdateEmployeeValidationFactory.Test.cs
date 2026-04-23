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
    public void Should_Return_Success_When_UserName_Valid()
    {
        var input = new UpdateEmployeeInput
        {
            UserName = "joao123"
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.UserName);
    }

    [Fact]
    public void Should_Return_Success_When_Password_Valid()
    {
        var input = new UpdateEmployeeInput
        {
            NewPassword = "Senha@123"
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Password);
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
    public void Should_Fail_When_UserName_Invalid()
    {
        var input = new UpdateEmployeeInput
        {
            UserName = ""
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Password_Invalid()
    {
        var input = new UpdateEmployeeInput
        {
            NewPassword = ""
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
            Phone = "",
            UserName = "",
            NewPassword = ""
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
    public void Should_Return_Success_When_All_Valid()
    {
        var input = new UpdateEmployeeInput
        {
            Name = "João",
            Email = "joao@email.com",
            Phone = "44999999999",
            UserName = "joao123",
            NewPassword = "Senha@123",
            Role = "Employee"
        };

        var result = UpdateEmployeeValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Name);
        Assert.NotNull(result.Value.Email);
        Assert.NotNull(result.Value.Phone);
        Assert.NotNull(result.Value.UserName);
        Assert.NotNull(result.Value.Password);
        Assert.NotNull(result.Value.Role);
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
        Assert.Null(result.Value.UserName);
        Assert.Null(result.Value.Password);
        Assert.Null(result.Value.Role);
    }
}