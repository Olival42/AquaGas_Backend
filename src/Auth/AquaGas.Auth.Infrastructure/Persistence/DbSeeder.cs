using AquaGas.Auth.Domain.Enums;
using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Services;
using AquaGas.Auth.Domain.ValueObjects;

using EmployeeEntity = AquaGas.Employee.Domain.Models.Employee;
using AquaGas.Employee.Domain.ValueObjects;

using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Employee.Infrastructure.Persistence;

namespace AquaGas.Auth.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AuthDbContext authContext,
        EmployeeDbContext employeeContext,
        IPasswordHasher hasher)
    {
        if (authContext.Users.Any())
            return;

        var employee = new EmployeeEntity(
            EmployeeName.Create("Gerente Inicial").Value!,
            Cpf.Create("52998224725").Value!,
            Email.Create("gerente@aquagas.local").Value!,
            Phone.Create("11999999999").Value!
        );

        await employeeContext.Employees.AddAsync(employee);
        await employeeContext.SaveChangesAsync();

        var user = new User(
            UserName.Create("gerente123").Value!,
            hasher.Hash("Admin@123"),
            Role.Manager,
            employee.Id
        );

        await authContext.Users.AddAsync(user);
        await authContext.SaveChangesAsync();
    }
}