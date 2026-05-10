using Xunit;
using AquaGas.Employee.Application.Validators;
using AquaGas.Employee.Application.Dtos.Requests;
using AquaGas.Auth.Application.Dtos.Requests;

public class RegisterEmployeeValidatorTests
{
    private readonly RegisterEmployeeValidator _validator = new();

    private RegisterEmployeeInput CreateValid()
    {
        return new RegisterEmployeeInput
        {
            User = new UserInput
            {
                UserName = "joao",
                Password = "123456",
                Role = "Employee"
            },
            Employee = new EmployeeInput
            {
                Name = "João",
                Cpf = "12345678909",
                Email = "joao@email.com",
                Phone = "44999999999"
            }
        };
    }

    [Fact]
    public void Should_Be_Valid()
    {
        var input = CreateValid();

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_User_Is_Null()
    {
        var input = CreateValid();
        input = input with { User = null! };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_Employee_Is_Null()
    {
        var input = CreateValid();
        input = input with { Employee = null! };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_Username_Is_Empty()
    {
        var valid = CreateValid();

        var input = valid with
        {
            User = valid.User with { UserName = "" }
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Empty()
    {
        var valid = CreateValid();

        var input = valid with
        {
            User = valid.User with { Password = "" }
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var valid = CreateValid();

        var input = valid with
        {
            Employee = valid.Employee with { Name = "" }
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_Cpf_Is_Empty()
    {
        var valid = CreateValid();

        var input = valid with
        {
            Employee = valid.Employee with { Cpf = "" }
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Empty()
    {
        var valid = CreateValid();

        var input = valid with
        {
            Employee = valid.Employee with { Email = "" }
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_Phone_Is_Empty()
    {
        var valid = CreateValid();

        var input = valid with
        {
            Employee = valid.Employee with { Phone = "" }
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
    }
}