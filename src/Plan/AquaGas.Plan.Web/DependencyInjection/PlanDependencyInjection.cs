using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Application.Validators;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Plan.Infrastructure.Persistence;
using AquaGas.Plan.Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;

using Microsoft.EntityFrameworkCore;
namespace AquaGas.Plan.Web.DependencyInjection;

public static class PlanDependencyInjection
{
    public static IServiceCollection AddPlanModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<PlanDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IContractPenaltyRepository, ContractPenaltyRepository>();

        services.AddScoped<IPlanDateService, PlanDateService>();
        services.AddScoped<IPlanCalculationService, PlanCalculationService>();
        services.AddScoped<IPlanDeliveryService, PlanDeliveryService>();
        services.AddScoped<IPlanBillingService, PlanBillingService>();
        services.AddScoped<IPlanFactoryService, PlanFactoryService>();
        services.AddScoped<IContractPenaltyService, ContractPenaltyService>();
        services.AddScoped<IPlanLifecycleService, PlanLifecycleService>();

        services.AddScoped<IRegisterPlan, RegisterPlan>();
        services.AddScoped<IConfirmDelivery, ConfirmDelivery>();
        services.AddScoped<ICancelDelivery, CancelDelivery>();
        services.AddScoped<IRescheduleDelivery, RescheduleDelivery>();
        services.AddScoped<IConfirmBillingPayment, ConfirmBillingPayment>();
        services.AddScoped<ISuspendPlan, SuspendPlan>();
        services.AddScoped<IReactivatePlan, ReactivatePlan>();
        services.AddScoped<ICancelPlan, CancelPlan>();
        services.AddScoped<IUpgradePlan, UpgradePlan>();
        services.AddScoped<IDowngradePlan, DowngradePlan>();
        services.AddScoped<IConfirmContractPenaltyPayment, ConfirmContractPenaltyPayment>();
        services.AddScoped<IWaiveContractPenalty, WaiveContractPenalty>();
        services.AddScoped<ICancelContractPenalty, CancelContractPenalty>();
        services.AddScoped<IGetById, GetById>();
        services.AddScoped<IGetAllPlans, GetAllPlans>();

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<RegisterPlanInputValidator>();
        services.AddValidatorsFromAssemblyContaining<RegisterPlanItemsInputValidator>();
        services.AddValidatorsFromAssemblyContaining<CancelDeliveryInputValidator>();
        services.AddValidatorsFromAssemblyContaining<ConfirmBillingPaymentInputValidator>();
        services.AddValidatorsFromAssemblyContaining<ConfirmDeliveryInputValidator>();
        services.AddValidatorsFromAssemblyContaining<RescheduleDeliveryInputValidator>();
        services.AddValidatorsFromAssemblyContaining<SuspendPlanInputValidator>();
        services.AddValidatorsFromAssemblyContaining<UpgradePlanInputValidator>();
        services.AddValidatorsFromAssemblyContaining<DowngradePlanInputValidator>();
        services.AddValidatorsFromAssemblyContaining<WaiveContractPenaltyInputValidator>();
        services.AddValidatorsFromAssemblyContaining<CancelContractPenaltyInputValidator>();

        return services;
    }
}
