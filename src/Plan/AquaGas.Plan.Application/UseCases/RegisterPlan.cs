using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Plan.Domain.Enums;

namespace AquaGas.Plan.Application.UseCases;

public sealed class RegisterPlan : IRegisterPlan
{
    private readonly IPlanRepository _planRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IContractPenaltyRepository _penaltyRepository;
    private readonly IUserContextService _userContext;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPlanDateService _dateService;
    private readonly IPlanCalculationService _calculationService;
    private readonly IPlanDeliveryService _deliveryService;
    private readonly IPlanBillingService _billingService;
    private readonly IPlanFactoryService _factoryService;
    private readonly IAuditLogService _auditLogService;

    public RegisterPlan(
        IPlanRepository planRepository,
        IDeliveryRepository deliveryRepository,
        IBillingRepository billingRepository,
        IContractPenaltyRepository penaltyRepository,
        IUserContextService userContext,
        IEmployeeRepository employeeRepository,
        IProductRepository productRepository,
        IPlanDateService dateService,
        IPlanCalculationService calculationService,
        IPlanDeliveryService deliveryService,
        IPlanBillingService billingService,
        IPlanFactoryService factoryService,
        ICustomerRepository customerRepository,
        IAuditLogService auditLogService)
    {
        _planRepository = planRepository;
        _deliveryRepository = deliveryRepository;
        _billingRepository = billingRepository;
        _penaltyRepository = penaltyRepository;
        _userContext = userContext;
        _employeeRepository = employeeRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _dateService = dateService;
        _calculationService = calculationService;
        _deliveryService = deliveryService;
        _billingService = billingService;
        _factoryService = factoryService;
        _auditLogService = auditLogService;
    }

    public async Task<Result<PlanResponse>> Execute(
        RegisterPlanInput input)
    {
        var validation = RegisterPlanValidationFactory.Combine(input);

        if (validation.IsFailure)
            return Result<PlanResponse>
                .Fail(validation.Errors.ToArray());

        var userIdResult = _userContext.GetUserId();

        if (userIdResult.IsFailure)
            return Result<PlanResponse>
                .Fail(userIdResult.Errors.ToArray());

        var currentUserName = _userContext.GetUserName();

        if (currentUserName.IsFailure)
            return Result<PlanResponse>.Fail(
                currentUserName.Errors.ToArray());

        var currentUserRole = _userContext.GetRole();

        if (currentUserRole.IsFailure)
            return Result<PlanResponse>.Fail(
                currentUserRole.Errors.ToArray());

        var employee =
            await _employeeRepository.GetByUserIdAsync(
                userIdResult.Value);

        if (employee is null)
            return Result<PlanResponse>
                .Fail(Error.NotFound("Employee not found"));

        var customer =
            await _customerRepository.GetByIdAsync(
                input.CustomerId);

        if (customer is null)
            return Result<PlanResponse>
                .Fail(Error.NotFound("Customer not found"));

        var penalties = await _penaltyRepository.GetByCustomerIdAsync(customer.Id);
            var hasOpenPenalty = penalties.Any(x => 
                x.Status == ContractPenaltyStatus.PendingPayment || 
                x.Status == ContractPenaltyStatus.Overdue);

        if (hasOpenPenalty)
            return Result<PlanResponse>.Fail(
                Error.Conflict("Customer has open contract penalties."));

        if (!input.IgnoreWarnings)
        {
            var debts = await _billingRepository.GetLateByCustomerIdAsync(customer.Id);
            if (debts.Any())
            {
                return Result<PlanResponse>.Fail(
                    Error.Conflict($"Customer has {debts.Count} overdue payments. Do you want to proceed anyway?"));
            }
        }

        if (validation.Value!.Discount is not null &&
            currentUserRole.Value == Role.Employee)
            return Result<PlanResponse>.Fail(
                Error.Forbidden(
                    "Employees cannot apply discounts; only managers can"));

        decimal monthlySubtotal = 0;
        var productNames = new Dictionary<Guid, string>();

        foreach (var item in input.Items)
        {
            var product =
                await _productRepository
                    .GetByIdAsync(item.ProductId);

            if (product is null)
                return Result<PlanResponse>
                    .Fail(Error.NotFound("Product not found"));

            monthlySubtotal += product.Price.Value * item.Quantity;
            productNames[product.Id] = product.Name.Value;
        }

        var startDate =
            _dateService.GenerateStartDate(
                input.DeliveryDay,
                input.BillingDay);

        var endDate =
            _dateService.GenerateEndDate(
                startDate,
                validation.Value!.Cycle,
                input.DeliveryDay,
                input.BillingDay,
                input.DurationInMonths);

        var tempBillings =
            _billingService.GenerateBillings(
                Guid.Empty,
                startDate,
                endDate,
                validation.Value.Cycle,
                input.BillingDay,
                Price.Create(1).Value!,
                input.DurationInMonths);

        int numberOfMonths = tempBillings.Count;

        var contractTotalResult =
            _calculationService.CalculateContractTotal(
                monthlySubtotal,
                numberOfMonths,
                validation.Value.Discount);

        if (contractTotalResult.IsFailure)
            return Result<PlanResponse>
                .Fail(contractTotalResult.Errors.ToArray());

        var contractTotal = contractTotalResult.Value!;

        var monthlyBillingResult =
            _calculationService.CalculateMonthlyBilling(
                contractTotal,
                numberOfMonths);

        if (monthlyBillingResult.IsFailure)
            return Result<PlanResponse>
                .Fail(monthlyBillingResult.Errors.ToArray());

        var monthlyBillingAmount = monthlyBillingResult.Value!;

        var plan =
            _factoryService.CreatePlan(
                input,
                employee.Id,
                validation.Value.Cycle,
                contractTotal,
                validation.Value.Discount,
                startDate,
                endDate,
                input.DeliveryDay,
                input.BillingDay);

        var items =
            _factoryService.CreateItems(
                plan.Id,
                validation.Value.Items);

        foreach (var item in items)
        {
            plan.AddItem(item);
        }

        var deliveries =
            _deliveryService.GenerateDeliveries(
                plan.Id,
                startDate,
                endDate,
                validation.Value.Cycle,
                input.DeliveryDay,
                input.DurationInMonths);

        var finalBillings =
            _billingService.GenerateBillings(
                plan.Id,
                startDate,
                endDate,
                validation.Value.Cycle,
                input.BillingDay,
                monthlyBillingAmount,
                input.DurationInMonths);

        await _planRepository.AddAsync(plan);

        foreach (var delivery in deliveries)
        {
            await _deliveryRepository.AddAsync(delivery);
        }

        foreach (var billing in finalBillings)
        {
            await _billingRepository.AddAsync(billing);
        }

        await _planRepository.SaveChangesAsync();

        await LogAudit(
            userIdResult.Value,
            _userContext.GetUserName().Value!,
            AuditAction.CREATE,
            plan);

        var response =
            new PlanResponse
            {
                Id = plan.Id,
                CustomerId = plan.CustomerId,
                CustomerName = customer.Name.Value,
                Document = customer.Document.Value,
                EmployeeId = plan.EmployeeId,
                EmployeeName = employee.Name.Value,
                Cycle = plan.Cycle,
                Total = plan.Total.Value,
                Discount = plan.CurrentDiscount?.Value,
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                Status = plan.Status,
                DeliveryDay = validation.Value.DeliveryDay,
                BillingDay = validation.Value.BillingDay,

                Items = plan.Items
                    .Select(x => new PlanItemResponse
                    {
                        ProductId = x.ProductId,
                        ProductName = productNames.GetValueOrDefault(x.ProductId, string.Empty),
                        Quantity = x.Quantity.Value
                    })
                    .ToList(),

                Deliveries = deliveries
                    .Select(x => new DeliveryResponse
                    {
                        Id = x.Id,
                        DueDate = x.DueDate,
                        Status = x.Status,
                        Period = x.Period
                    })
                    .ToList(),

                Billings = finalBillings
                    .Select(x => new BillingResponse
                    {
                        Id = x.Id,
                        DueDate = x.DueDate,
                        Amount = x.Amount.Value,
                        Status = x.Status
                    })
                    .ToList()
            };

        return Result<PlanResponse>
            .Success(response);
    }

    private async Task LogAudit(
        Guid userId,
        string userName,
        AuditAction action,
        Domain.Models.Plan plan)
    {
        await _auditLogService.LogAsync(
            userId,
            userName,
            action,
            "Plan",
            plan.Id,
            null,
            new
            {
                plan.CustomerId,
                plan.EmployeeId,
                plan.Cycle,
                Total = plan.Total.Value,
                plan.StartDate,
                plan.EndDate,
                Items = plan.Items.Select(i => new
                {
                    i.ProductId,
                    Quantity = i.Quantity.Value
                })
            });
    }
}