using Xunit;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;

public class ResetPasswordValidationTests
{
    [Fact]
    public void Should_Return_Success_When_Password_Is_Valid()
    {
        var input = new ResetPasswordInput
        {
            NewPassword = "Senha@123"
        };

        var result = ResetPasswordValidation.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value!.Password);
    }

    [Fact]
    public void Should_Return_Failure_When_Password_Is_Invalid()
    {
        var input = new ResetPasswordInput
        {
            NewPassword = "123"
        };

        var result = ResetPasswordValidation.Combine(input);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Should_Return_Error_When_Password_Is_Null()
    {
        var input = new ResetPasswordInput
        {
            NewPassword = null!
        };

        var result = ResetPasswordValidation.Combine(input);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Should_Contain_Password_In_Result_When_Valid()
    {
        var input = new ResetPasswordInput
        {
            NewPassword = "SenhaForte@123"
        };

        var result = ResetPasswordValidation.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("SenhaForte@123", result.Value!.Password.Value);
    }
}