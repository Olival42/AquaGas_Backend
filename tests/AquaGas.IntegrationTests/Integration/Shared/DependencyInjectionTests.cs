using AquaGas.Auth.Application.UseCase;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Services;
using AquaGas.Auth.Infrastructure.Persistence;
using AquaGas.Customer.Application.UseCases;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Customer.Infrastructure.Persistence;
using AquaGas.Employee.Application.UseCases;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Employee.Infrastructure.Persistence;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Plan.Infrastructure.Persistence;
using AquaGas.Product.Application.UseCases;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Infrastructure.Persistence;
using AquaGas.Sale.Application.UseCases;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Sale.Infrastructure.Persistence;
using AquaGas.Shared.Application.Repositories;
using AquaGas.Shared.Infrastructure.Cache;
using AquaGas.Shared.Infrastructure.Persistence;
using AquaGas.Shared.Infrastructure.TokenBlacklist;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace AquaGas.IntegrationTests.Integration.Shared;

[Collection("Integration")]
public class DependencyInjectionTests
{
    private readonly CustomWebApplicationFactory _factory;

    public DependencyInjectionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(typeof(AuthDbContext))]
    [InlineData(typeof(EmployeeDbContext))]
    [InlineData(typeof(CustomerDbContext))]
    [InlineData(typeof(ProductDbContext))]
    [InlineData(typeof(SaleDbContext))]
    [InlineData(typeof(PlanDbContext))]
    [InlineData(typeof(AppDbContext))]
    public void DbContexts_AreRegistered(Type dbContextType)
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService(dbContextType);
        service.Should().NotBeNull($"{dbContextType.Name} should be registered");
    }

    [Theory]
    [InlineData(typeof(IUserRepository))]
    [InlineData(typeof(IRefreshTokenRepository))]
    [InlineData(typeof(ICustomerRepository))]
    [InlineData(typeof(IEmployeeRepository))]
    [InlineData(typeof(IProductRepository))]
    [InlineData(typeof(ISaleRepository))]
    [InlineData(typeof(IPlanRepository))]
    [InlineData(typeof(IDeliveryRepository))]
    [InlineData(typeof(IBillingRepository))]
    [InlineData(typeof(IContractPenaltyRepository))]
    [InlineData(typeof(IAuditLogRepository))]
    public void Repositories_AreRegistered(Type repoType)
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService(repoType);
        service.Should().NotBeNull($"{repoType.Name} should be registered");
    }

    [Theory]
    [InlineData(typeof(IPasswordHasher))]
    [InlineData(typeof(IJwtService))]
    [InlineData(typeof(IRedisService))]
    [InlineData(typeof(ITokenBlacklistService))]
    public void SharedServices_AreRegistered(Type serviceType)
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService(serviceType);
        service.Should().NotBeNull($"{serviceType.Name} should be registered");
    }

    [Theory]
    [InlineData(typeof(ILogin))]
    [InlineData(typeof(ILogout))]
    [InlineData(typeof(IRefresh))]
    [InlineData(typeof(IRegisterCustomer))]
    [InlineData(typeof(IGetAllCustomers))]
    [InlineData(typeof(IRegisterEmployee))]
    [InlineData(typeof(IGetAllEmployees))]
    [InlineData(typeof(IRegisterProduct))]
    [InlineData(typeof(IGetAllProducts))]
    [InlineData(typeof(IRegisterSale))]
    [InlineData(typeof(IGetAllSales))]
    [InlineData(typeof(IRegisterPlan))]
    [InlineData(typeof(IGetAllPlans))]
    public void UseCases_AreRegistered(Type useCaseType)
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService(useCaseType);
        service.Should().NotBeNull($"{useCaseType.Name} should be registered");
    }
}
