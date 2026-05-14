using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.UseCases;
using AquaGas.Sale.Application.Validations;
using AquaGas.Sale.Application.Validators;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Sale.Infrastructure.Persistence;
using AquaGas.Sale.Infrastructure.Persistence.Repositories;

using FluentValidation;
using FluentValidation.AspNetCore;

using Microsoft.EntityFrameworkCore;
namespace AquaGas.Sale.Web.DependencyInjection;

public static class SaleDependencyInjection
{
    public static IServiceCollection AddSaleModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<SaleDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection")));

        services.AddScoped<ISaleRepository, SaleRepository>();

        services.AddScoped<IRegisterSale, RegisterSale>();
        services.AddScoped<IGetById, GetById>();
        services.AddScoped<IGetAllSales, GetAllSales>();
        services.AddScoped<ICancelSale, CancelSale>();

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<RegisterSaleValidator>();
        services.AddValidatorsFromAssemblyContaining<CancelSaleValidator>();

        return services;
    }
}
