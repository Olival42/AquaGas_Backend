using Xunit;
using Mapster;
using AquaGas.Employee.Application.Mappings;
using AquaGas.Employee.Domain.Models;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Employee.Application.Dtos.Responses;

public class EmployeeMappingTests
{
    private readonly TypeAdapterConfig _config;

    public EmployeeMappingTests()
    {
        _config = new TypeAdapterConfig();
        EmployeeMapping.Register(_config);
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

        var result = employee.Adapt<EmployeeResponse>(_config);

        Assert.Equal(employee.Id, result.Id);
        Assert.Equal(employee.Name.Value, result.Name);
        Assert.Equal(employee.CPF.Value, result.Cpf);
        Assert.Equal(employee.Email.Value, result.Email);
        Assert.Equal(employee.Phone.Value, result.Phone);
    }
}
