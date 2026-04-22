using Xunit;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;

public class RegisterEmployeeValidationFactoryTests
{
    [Fact]
    public void Should_Return_Success_When_All_Data_Is_Valid()
    {
        var input = new RegisterEmployeeInput
        {
            Employee = new()
            {
                Name = "João Silva",
                Cpf = "12345678909",
                Email = "joao@email.com",
                Phone = "44999999999"
            },
            User = new()
            {
                UserName = "joao",
                Password = "Senha@123",
                Role = "Employee"
            }
        };

        var result = RegisterEmployeeValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void Should_Fail_When_Name_Invalid()
    {
        var input = new RegisterEmployeeInput
        {
            Employee = new()
            {
                Name = "",
                Cpf = "12345678909",
                Email = "email@email.com",
                Phone = "44999999999"
            },
            User = new()
            {
                UserName = "user",
                Password = "Senha@123",
                Role = "Employee"
            }
        };

        var result = RegisterEmployeeValidationFactory.Combine(input);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Role_Invalid()
    {
        var input = new RegisterEmployeeInput
        {
            Employee = new()
            {
                Name = "João",
                Cpf = "12345678909",
                Email = "email@email.com",
                Phone = "44999999999"
            },
            User = new()
            {
                UserName = "user",
                Password = "Senha@123",
                Role = "999"
            }
        };

        var result = RegisterEmployeeValidationFactory.Combine(input);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Return_Multiple_Errors_When_Many_Invalid()
    {
        var input = new RegisterEmployeeInput
        {
            Employee = new()
            {
                Name = "",
                Cpf = "",
                Email = "email-invalido",
                Phone = ""
            },
            User = new()
            {
                UserName = "",
                Password = "",
                Role = "999"
            }
        };

        var result = RegisterEmployeeValidationFactory.Combine(input);

        Assert.False(result.IsSuccess);
        Assert.True(result.Errors.Count > 1);
    }
}