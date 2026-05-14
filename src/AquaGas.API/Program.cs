using System.Text.Json.Serialization;
using AquaGas.Auth.Application.Mappings;
using AquaGas.Auth.Domain.Services;
using AquaGas.Auth.Infrastructure.Persistence;
using AquaGas.Auth.Infrastructure.Security;
using AquaGas.Customer.Application.Mappings;
using AquaGas.Employee.Application.Mappings;
using AquaGas.Product.Application.Mappings;
using AquaGas.Shared.Filters;
using AquaGas.Shared.Infrastructure.DependencyInjection;
using AquaGas.Shared.Middlewares;
using AquaGas.Shared.Security;

using FluentValidation.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using StackExchange.Redis;

using AquaGas.Customer.Web.DependencyInjection;
using AquaGas.Employee.Web.DependencyInjection;
using AquaGas.Product.Web.DependencyInjection;
using AquaGas.Auth.Web.DependencyInjection;
using AquaGas.Sale.Web.DependencyInjection;
using AquaGas.Employee.Infrastructure.Persistence;
using AquaGas.Customer.Infrastructure.Persistence;
using AquaGas.Product.Infrastructure.Persistence;
using AquaGas.Sale.Infrastructure.Persistence;
using AquaGas.Shared.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services
    .AddControllers(options =>
    {
        options.ModelValidatorProviders.Clear();
        options.Filters.Add<ValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var configuration =
        builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException(
            "Redis connection missing");

    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            TokenValidationConfig
                .GetTokenValidationParameters(builder.Configuration);

        options.Events = JwtEventsConfig.GetEvents();
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSharedServices();

builder.Services.AddSharedRepositories(builder.Configuration);

builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddEmployeeModule(builder.Configuration);
builder.Services.AddCustomerModule(builder.Configuration);
builder.Services.AddProductModule(builder.Configuration);
builder.Services.AddSaleModule(builder.Configuration);

builder.Services.AddFluentValidationAutoValidation();

UserMapping.Register();
EmployeeMapping.Register();
CustomerMapping.Register();
ProductMapping.Register();

builder.Services.Configure<Argon2Options>(
    builder.Configuration.GetSection("Security:Argon2"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();

    var authDb =
        scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    var employeeDb =
        scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();

    var customerDb =
        scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

    var productDb =
        scope.ServiceProvider.GetRequiredService<ProductDbContext>();

    var saleDb =
        scope.ServiceProvider.GetRequiredService<SaleDbContext>();

    var appDb =
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await authDb.Database.MigrateAsync();
    await employeeDb.Database.MigrateAsync();
    await customerDb.Database.MigrateAsync();
    await productDb.Database.MigrateAsync();
    await saleDb.Database.MigrateAsync();
    await appDb.Database.MigrateAsync();

    var hasher =
        scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    await DbSeeder.SeedAsync(
        authDb,
        employeeDb,
        hasher);
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();

app.UseMiddleware<TokenBlacklistMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }