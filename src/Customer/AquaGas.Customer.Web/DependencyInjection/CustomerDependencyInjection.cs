using AquaGas.Customer.Application.UseCases;
using AquaGas.Customer.Application.Validators;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Customer.Infrastructure.Persistence;
using AquaGas.Customer.Infrastructure.Persistence.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

namespace AquaGas.Customer.Web.DependencyInjection;

public static class CustomerDependencyInjection
{
    public static IServiceCollection AddCustomerModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<CustomerDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection")));

        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddScoped<IRegisterCustomer, RegisterCustomer>();
        services.AddScoped<IExistsCustomerByDocument, ExistsCustomerByDocument>();
        services.AddScoped<IGetByCustomerId, GetByCustomerId>();
        services.AddScoped<IGetAllCustomers, GetAllCustomers>();
        services.AddScoped<IUpdateCustomer, UpdateCustomer>();
        services.AddScoped<IDeactiveCustomer, DeactiveCustomer>();

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<RegisterCustomerValidator>();

        return services;
    }
}
