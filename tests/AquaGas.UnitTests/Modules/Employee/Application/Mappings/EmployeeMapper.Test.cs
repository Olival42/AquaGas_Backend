using Xunit;
using Mapster;
using AquaGas.API.Modules.Employee.Application.Mappings;
using AquaGas.Api.Modules.Employee.Domain.Models;
using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;

public class EmployeeMappingTests
{
    public EmployeeMappingTests()
    {
        EmployeeMapping.Register();
    }

    [Fact]
    public void Should_Map_Employee_To_EmployeeResponse()
    {
        var employee = new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("05244777017").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        var result = employee.Adapt<EmployeeResponse>();

        Assert.Equal(employee.Id, result.Id);
        Assert.Equal(employee.Name.Value, result.Name);
        Assert.Equal(employee.CPF.Value, result.Cpf);
        Assert.Equal(employee.Email.Value, result.Email);
        Assert.Equal(employee.Phone.Value, result.Phone);
    }
}