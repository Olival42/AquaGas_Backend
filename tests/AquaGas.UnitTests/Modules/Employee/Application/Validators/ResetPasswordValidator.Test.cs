using Xunit;
using AquaGas.Api.Modules.Employee.Application.Validators;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;

public class ResetPasswordValidatorTests
{
    private readonly ResetPasswordValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Password_Is_Valid()
    {
        var input = new ResetPasswordInput
        {
            NewPassword = "Senha@123"
        };

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Empty()
    {
        var input = new ResetPasswordInput
        {
            NewPassword = ""
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => 
            e.ErrorMessage == "New password is required");
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Null()
    {
        var input = new ResetPasswordInput
        {
            NewPassword = null!
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Override_Property_Name()
    {
        var input = new ResetPasswordInput
        {
            NewPassword = ""
        };

        var result = _validator.Validate(input);

        Assert.Equal("New Password", result.Errors[0].PropertyName);
    }
}