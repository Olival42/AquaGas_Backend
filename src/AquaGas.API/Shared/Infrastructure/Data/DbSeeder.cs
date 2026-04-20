namespace AquaGas.Api.Shared.Infrastructure.Persistence;

using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Modules.Employee.Domain.Models;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;


public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher hasher)
    {
        if (!context.Users.Any())
        {
            var employee = new Employee(
                EmployeeName.Create("Gerente Inicial"),
                Cpf.Create("52998224725").Value!,
                Email.Create("gerente@aquagas.local").Value!,
                Phone.Create("11999999999").Value!
            );

            var usuario = new User(
                UserName.Create("gerente123").Value!,
                hasher.Hash("Admin@123"),
                Role.MANAGER,
                employee.Id
            );

            context.Employees.Add(employee);
            context.Users.Add(usuario);
            await context.SaveChangesAsync();
        }
    }
}
