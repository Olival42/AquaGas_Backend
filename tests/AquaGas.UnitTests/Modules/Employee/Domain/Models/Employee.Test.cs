using Xunit;
using AquaGas.Api.Modules.Employee.Domain.Models;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class EmployeeTests
{
    private Employee CreateEmployee()
    {
        return new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );
    }

    private User CreateUser(Employee employee)
    {
        return new User(
            UserName.Create("joao").Value!,
            "hash",
            Role.Employee,
            employee.Id
        );
    }

    [Fact]
    public void Should_Create_Employee_As_Active()
    {
        var employee = CreateEmployee();

        Assert.True(employee.IsActive);
        Assert.NotEqual(Guid.Empty, employee.Id);
    }

    [Fact]
    public void Should_Deactivate_Employee()
    {
        var employee = CreateEmployee();

        employee.Deactive();

        Assert.False(employee.IsActive);
    }

    [Fact]
    public void Should_Deactivate_User_When_Deactivating_Employee()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        employee.AssignUser(user);

        employee.Deactive();

        Assert.False(employee.IsActive);
        Assert.False(user.IsActive);
    }

    [Fact]
    public void Should_Update_Name()
    {
        var employee = CreateEmployee();

        var newName = EmployeeName.Create("Maria").Value!;

        employee.Update(newName, null, null);

        Assert.Equal("Maria", employee.Name.Value);
    }

    [Fact]
    public void Should_Update_Email()
    {
        var employee = CreateEmployee();

        var newEmail = Email.Create("maria@email.com").Value!;

        employee.Update(null, newEmail, null);

        Assert.Equal("maria@email.com", employee.Email.Value);
    }

    [Fact]
    public void Should_Update_Phone()
    {
        var employee = CreateEmployee();

        var newPhone = Phone.Create("44988888888").Value!;

        employee.Update(null, null, newPhone);

        Assert.Equal("44988888888", employee.Phone.Value);
    }

    [Fact]
    public void Should_Not_Update_When_All_Null()
    {
        var employee = CreateEmployee();

        var originalName = employee.Name.Value;

        employee.Update(null, null, null);

        Assert.Equal(originalName, employee.Name.Value);
    }
}