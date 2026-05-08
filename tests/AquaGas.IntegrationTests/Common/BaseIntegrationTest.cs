using System.Net.Http.Headers;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.IntegrationTests.Collections;
using AquaGas.IntegrationTests.Fixtures;
using AquaGas.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace AquaGas.IntegrationTests.Common;

[Collection(IntegrationTestCollection.Name)]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;

    protected BaseIntegrationTest(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    protected HttpClient CreateClient() => _fixture.Factory.CreateClient();

    protected async Task<T> ExecuteDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(dbContext);
    }

    protected async Task ExecuteDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(dbContext);
    }

    protected HttpClient CreateAuthenticatedClient(
    string role = "Manager",
    Guid? userId = null,
    string? userName = null)
    {
        var client = CreateClient();

        var token = AuthTokenHelper.CreateAccessToken(
            _fixture.Factory.Configuration,
            role,
            userName,
            userId);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    protected async Task<(Guid UserId, string UserName)> SeedUserAsync(string userName = "testuser", string role = "Manager")
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            AquaGas.Api.Modules.Employee.Domain.ValueObjects.EmployeeName.Create("Test Employee").Value!,
            AquaGas.Api.Shared.Domain.ValueObjects.Cpf.Create("12345678909").Value!,
            AquaGas.Api.Shared.Domain.ValueObjects.Email.Create($"{userName}@test.com").Value!,
            AquaGas.Api.Shared.Domain.ValueObjects.Phone.Create("11999999999").Value!
        );

        var user = new AquaGas.Api.Modules.Auth.Domain.Models.User(
            AquaGas.Api.Modules.Auth.Domain.ValueObjects.UserName.Create(userName).Value!,
            "hashed_password",
            Enum.Parse<AquaGas.Api.Modules.Auth.Domain.Enums.Role>(role),
            employee.Id
        );

        db.Employees.Add(employee);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (user.Id, user.UserName.Value);
    }

    public virtual async Task InitializeAsync()
    {
        await _fixture.Factory.ResetDatabaseAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;
}
