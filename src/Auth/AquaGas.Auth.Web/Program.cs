using AquaGas.Auth.Web.DependencyInjection;
using AquaGas.Shared.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Shared (sempre primeiro)
builder.Services.AddSharedServices();
builder.Services.AddSharedRepositories(builder.Configuration);

// Módulo Auth
builder.Services.AddAuthModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
public partial class Program { }