using AquaGas.Customer.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public sealed class GetById : IGetById
{
    private readonly IPlanRepository _planRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IContractPenaltyRepository _penaltyRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IProductRepository _productRepository;

    public GetById(
        IPlanRepository planRepository,
        IDeliveryRepository deliveryRepository,
        IBillingRepository billingRepository,
        IContractPenaltyRepository penaltyRepository,
        ICustomerRepository customerRepository,
        IEmployeeRepository employeeRepository,
        IProductRepository productRepository)
    {
        _planRepository = planRepository;
        _deliveryRepository = deliveryRepository;
        _billingRepository = billingRepository;
        _penaltyRepository = penaltyRepository;
        _customerRepository = customerRepository;
        _employeeRepository = employeeRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<PlanResponse>> Execute(Guid id)
    {
        var plan =
            await _planRepository.GetByIdAsync(id);

        if (plan is null)
            return Result<PlanResponse>
                .Fail(Error.NotFound("Plan not found"));

        var customer =
            await _customerRepository
                .GetByIdAsync(plan.CustomerId, false);

        if (customer is null)
            return Result<PlanResponse>
                .Fail(Error.NotFound("Customer not found"));

        var employee =
            await _employeeRepository
                .GetByIdAsync(plan.EmployeeId, false);

        if (employee is null)
            return Result<PlanResponse>
                .Fail(Error.NotFound("Employee not found"));

        var deliveries =
            await _deliveryRepository
                .GetByPlanIdAsync(plan.Id);

        var billings =
            await _billingRepository
                .GetByPlanIdAsync(plan.Id);

        bool changed = false;
        foreach (var delivery in deliveries)
        {
            var previousStatus = delivery.Status;
            delivery.MarkAsLate();
            if (delivery.Status != previousStatus)
            {
                _deliveryRepository.Update(delivery);
                changed = true;
            }
        }

        foreach (var billing in billings)
        {
            var previousStatus = billing.Status;
            billing.MarkAsLate();
            if (billing.Status != previousStatus)
            {
                _billingRepository.Update(billing);
                changed = true;
            }
        }

        var penalties = await _penaltyRepository.GetByPlanIdAsync(plan.Id);
        foreach (var penalty in penalties)
        {
            var previousStatus = penalty.Status;
            penalty.MarkAsOverdue();
            if (penalty.Status != previousStatus)
            {
                _penaltyRepository.Update(penalty);
                changed = true;
            }
        }

        if (changed)
        {
            await _planRepository.SaveChangesAsync();
        }

        var itemOutputs =
            new List<PlanItemResponse>();

        foreach (var item in plan.Items)
        {
            var product =
                await _productRepository
                    .GetByIdAsync(item.ProductId);

            itemOutputs.Add(new PlanItemResponse
            {
                ProductId = item.ProductId,
                ProductName = product?.Name.Value ?? string.Empty,
                Quantity = item.Quantity.Value
            });
        }

        var deliveryOutputs =
            deliveries.Select(x =>
                new DeliveryResponse
                {
                    Id = x.Id,
                    Period = x.Period,
                    DueDate = x.DueDate,
                    DeliveryDate = x.DeliveryDate,
                    Status = x.Status
                })
            .ToList();

        var billingOutputs =
            billings.Select(x =>
                new BillingResponse
                {
                    Id = x.Id,
                    DueDate = x.DueDate,
                    Amount = x.Amount.Value,
                    Status = x.Status,
                    PaidAt = x.PaidAt,
                    ReceivedBy = x.ReceivedBy
                })
            .ToList();

        var penaltyOutputs =
            penalties.Select(x =>
                new PenaltyResponse
                {
                    Id = x.Id,
                    PlanId = x.PlanId,
                    Type = x.Type,
                    OriginalValue = x.OriginalValue.Value,
                    RemainingValue = x.RemainingValue.Value,
                    CalculatedAmount = x.CalculatedAmount.Value,
                    Status = x.Status,
                    Timestamp = x.Timestamp,
                    DueDate = x.DueDate,
                    PaidDate = x.PaidDate,
                    PaidBy = x.PaidBy,
                    WaivedBy = x.WaivedBy,
                    WaivedAt = x.WaivedAt,
                    WaiveReason = x.WaiveReason,
                    CanceledBy = x.CanceledBy,
                    CanceledAt = x.CanceledAt,
                    CancelReason = x.CancelReason,
                    Notes = x.Notes
                })
            .ToList();

        var output =
            new PlanResponse
            {
                Id = plan.Id,
                CustomerId = customer.Id,
                CustomerName = customer.Name.Value,
                Document = customer.Document.Value,
                EmployeeId = employee.Id,
                EmployeeName = employee.Name.Value,
                Cycle = plan.Cycle,
                Status = plan.Status,
                Total = plan.Total.Value,
                Discount = plan.CurrentDiscount?.Value,
                DeliveryDay = plan.DeliveryDay,
                BillingDay = plan.BillingDay,
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                Items = itemOutputs,
                Deliveries = deliveryOutputs,
                Billings = billingOutputs,
                Penalties = penaltyOutputs
            };

        return Result<PlanResponse>
            .Success(output);
    }
}