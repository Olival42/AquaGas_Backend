using Xunit;
using FluentValidation.TestHelper;
using AquaGas.Auth.Application.Validators;
using AquaGas.Auth.Application.Dtos.Requests;

public class LoginInputValidatorTests
{
    private readonly LoginInputValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Input_Is_Valid()
    {
        var model = new LoginInput
        {
            UserName = "john123",
            Password = "Abc@1234"
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_UserName_Is_Empty()
    {
        var model = new LoginInput
        {
            UserName = "",
            Password = "Abc@1234"
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Empty()
    {
        var model = new LoginInput
        {
            UserName = "john123",
            Password = ""
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Return_Custom_Error_Messages()
    {
        var model = new LoginInput
        {
            UserName = "",
            Password = ""
        };

        var result = _validator.TestValidate(model);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "UserName is required");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Password is required");
    }
}