using AquaGas.Api.Modules.Auth.Infrastructure.Security;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.Api.Shared.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using AquaGas.Api.Shared.Infrastructure.DependencyInjection;
using AquaGas.Api.Shared.Infrastructure.Mappings;
using AquaGas.Api.Shared.Filters;
using AquaGas.Api.Shared.Security;
using AquaGas.Api.Modules.Auth.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        options.Filters.Add<CustomValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException(
            "Connection string 'Redis' is missing. Set ConnectionStrings:Redis or environment variable ConnectionStrings__Redis (e.g. redis:6379 in Docker).");

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
    .AddRepositories()
    .AddCache()
    .AddSecurity()
    .AddValidation()
    .AddStandardApiBehavior();

builder.Services.Configure<Argon2Options>(
    builder.Configuration.GetSection("Security:Argon2"));

MapsterConfig.RegisterMappings();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
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
