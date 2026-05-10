using AquaGas.Employee.Domain.Models;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;
using Xunit;

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

    [Fact]
    public void Should_Create_Employee_As_Active()
    {
        var employee = CreateEmployee();

        Assert.True(employee.IsActive);
        Assert.NotEqual(Guid.Empty, employee.Id);
    }

    [Fact]
    public void Should_Assign_User_Id()
    {
        var employee = CreateEmployee();

        var userId = Guid.NewGuid();

        employee.AssignUserId(userId);

        Assert.Equal(userId, employee.UserId);
    }

    [Fact]
    public void Should_Deactivate_Employee()
    {
        var employee = CreateEmployee();

        employee.Deactivate();

        Assert.False(employee.IsActive);
    }

    [Fact]
    public void Should_Reactivate_Employee()
    {
        var employee = CreateEmployee();

        employee.Deactivate();
        employee.Reactivate();

        Assert.True(employee.IsActive);
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
        var originalEmail = employee.Email.Value;
        var originalPhone = employee.Phone.Value;

        employee.Update(null, null, null);

        Assert.Equal(originalName, employee.Name.Value);
        Assert.Equal(originalEmail, employee.Email.Value);
        Assert.Equal(originalPhone, employee.Phone.Value);
    }
}