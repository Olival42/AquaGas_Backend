using AquaGas.Api.Modules.Auth.Infrastructure.Security;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.Api.Shared.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using AquaGas.Api.Shared.Infrastructure.DependencyInjection;
using AquaGas.Api.Shared.Infrastructure.Mappings;
using AquaGas.Api.Shared.Security;
using AquaGas.Api.Modules.Auth.Application.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddControllers(options =>
{
    options.ModelValidatorProviders.Clear();
    options.Filters.Add<ValidationFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        var configuration = builder.Configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection missing");

        return ConnectionMultiplexer.Connect(configuration);
    });

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            TokenValidationConfig.GetTokenValidationParameters(builder.Configuration);

        options.Events = JwtEventsConfig.GetEvents();
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddServices()
    .AddRepositories(builder.Configuration)
    .AddCache()
    .AddSecurity()
    .AddValidation();

builder.Services.Configure<Argon2Options>(
    builder.Configuration.GetSection("Security:Argon2"));

MapsterConfig.RegisterMappings();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!builder.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, hasher);
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }