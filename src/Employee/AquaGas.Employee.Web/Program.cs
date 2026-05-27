using AquaGas.Employee.Web.DependencyInjection;
using AquaGas.Shared.Infrastructure.DependencyInjection;
using AquaGas.Shared.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAquaGasSwagger();

builder.Services.AddSharedServices();
builder.Services.AddSharedRepositories(builder.Configuration);

builder.Services.AddEmployeeModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseAquaGasSwagger();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }