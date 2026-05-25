using AquaGas.Report.Application.Configuration;
using AquaGas.Report.Application.UseCases;
using AquaGas.Report.Application.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace AquaGas.Report.Web.DependencyInjection;

public static class ReportDependencyInjection
{
    public static IServiceCollection AddReportModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings =
            configuration
                .GetSection("ReportSettings")
                .Get<ReportSettings>()
            ?? new ReportSettings();

        services.AddSingleton<IReportSettings>(settings);
        services.AddScoped<IGetStockMovementReport, GetStockMovementReport>();
        services.AddScoped<IGetSalesReport, GetSalesReport>();
        services.AddScoped<IGetContractPenaltyReport, GetContractPenaltyReport>();

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<StockMovementReportInputValidator>();
        services.AddValidatorsFromAssemblyContaining<SalesReportInputValidator>();
        services.AddValidatorsFromAssemblyContaining<ContractPenaltyReportInputValidator>();

        return services;
    }
}
