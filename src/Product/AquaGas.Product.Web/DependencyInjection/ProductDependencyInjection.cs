using AquaGas.Product.Infrastructure.Persistence.Repositories;
using AquaGas.Product.Application.Repositories;
using AquaGas.Product.Application.UseCases;

using AquaGas.Product.Domain.Repositories;

using AquaGas.Product.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace AquaGas.Product.Web.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddProductModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<ProductDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();

        services.AddScoped<IRegisterProduct, RegisterProduct>();
        services.AddScoped<IGetByProductId, GetByProductId>();
        services.AddScoped<IGetAllProducts, GetAllProducts>();
        services.AddScoped<IUpdateProduct, UpdateProduct>();
        services.AddScoped<IDeactiveProduct, DeactiveProduct>();
        services.AddScoped<IUpdateStock, UpdateStock>();

        return services;
    }
}