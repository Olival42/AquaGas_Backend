using FluentAssertions;
using Mapster;
using AquaGas.API.Modules.Employee.Application.Mappings;
using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using EmployeeEntity = AquaGas.Api.Modules.Employee.Domain.Models.Employee;
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
        var employee = new EmployeeEntity(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var response = employee.Adapt<EmployeeResponse>();

        response.Should().NotBeNull();

        response.Id.Should().Be(employee.Id);

        response.Name.Should().Be(employee.Name.Value);

        response.Cpf.Should().Be(employee.CPF.Value);

        response.Email.Should().Be(employee.Email.Value);

        response.Phone.Should().Be(employee.Phone.Value);
    }

    [Fact]
    public void Should_Map_Employee_Id_Correctly()
    {
        var employee = new EmployeeEntity(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var response = employee.Adapt<EmployeeResponse>();

        response.Id.Should().Be(employee.Id);
    }

    [Fact]
    public void Should_Map_Employee_Name_Correctly()
    {
        var employee = new EmployeeEntity(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var response = employee.Adapt<EmployeeResponse>();

        response.Name.Should().Be(employee.Name.Value);
    }

    [Fact]
    public void Should_Map_Employee_CPF_Correctly()
    {
        var employee = new EmployeeEntity(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var response = employee.Adapt<EmployeeResponse>();

        response.Cpf.Should().Be(employee.CPF.Value);
    }

    [Fact]
    public void Should_Map_Employee_Email_Correctly()
    {
        var employee = new EmployeeEntity(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var response = employee.Adapt<EmployeeResponse>();

        response.Email.Should().Be(employee.Email.Value);
    }

    [Fact]
    public void Should_Map_Employee_Phone_Correctly()
    {
        var employee = new EmployeeEntity(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var response = employee.Adapt<EmployeeResponse>();

        response.Phone.Should().Be(employee.Phone.Value);
    }

    [Fact]
    public void Should_Map_Multiple_Employees()
    {
        var employees = new List<EmployeeEntity>
        {
            new(
                EmployeeName.Create("John Doe").Value!,
                Cpf.Create("12345678909").Value!,
                Email.Create("john@test.com").Value!,
                Phone.Create("11999999999").Value!
            ),
            new(
                EmployeeName.Create("Jane Doe").Value!,
                Cpf.Create("98765432100").Value!,
                Email.Create("jane@test.com").Value!,
                Phone.Create("11888888888").Value!
            )
        };

        var responses = employees.Adapt<List<EmployeeResponse>>();

        responses.Should().HaveCount(2);

        responses[0].Name.Should().Be(employees[0].Name.Value);
        responses[0].Cpf.Should().Be(employees[0].CPF.Value);

        responses[1].Name.Should().Be(employees[1].Name.Value);
        responses[1].Cpf.Should().Be(employees[1].CPF.Value);
    }
}