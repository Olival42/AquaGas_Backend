using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Application.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.Dtos.Responses;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Report.Application.UseCases;

public sealed class GetStockMovementReport
    : IGetStockMovementReport
{
    private readonly IUserContextService _userContextService;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IPlanRepository _planRepository;

    public GetStockMovementReport(
        IUserContextService userContextService,
        IStockMovementRepository stockMovementRepository,
        IProductRepository productRepository,
        IEmployeeRepository employeeRepository,
        ICustomerRepository customerRepository,
        ISaleRepository saleRepository,
        IPlanRepository planRepository)
    {
        _userContextService = userContextService;
        _stockMovementRepository = stockMovementRepository;
        _productRepository = productRepository;
        _employeeRepository = employeeRepository;
        _customerRepository = customerRepository;
        _saleRepository = saleRepository;
        _planRepository = planRepository;
    }

    public async Task<Result<StockMovementReportResponse>> Execute(
        StockMovementReportInput input)
    {
        var currentUserRole = _userContextService.GetRole();

        if (currentUserRole.IsFailure)
            return Result<StockMovementReportResponse>.Fail(
                currentUserRole.Errors.ToArray());

        if (currentUserRole.Value != Role.Manager)
            return Result<StockMovementReportResponse>.Fail(
                Error.Forbidden("Only managers can access stock movement reports"));

        var startUtc = NormalizeUtc(input.Start);
        var endUtc = NormalizeUtc(input.End);

        if (startUtc > endUtc)
            return Result<StockMovementReportResponse>.Fail(
                Error.Validation(
                    "Start must be less than or equal to End",
                    "Period"));

        var movements = await _stockMovementRepository.GetReportAsync(
            startUtc,
            endUtc);

        var products = (await _productRepository.GetByIdsAsync(
            movements.Select(x => x.ProductId),
            false))
            .ToDictionary(x => x.Id);

        var employees = (await _employeeRepository.GetByUserIdsAsync(
            movements.Select(x => x.CreatedBy),
            false))
            .ToDictionary(x => x.UserId);

        var referenceIds = movements
            .Where(x => x.ReferenceId.HasValue)
            .Select(x => x.ReferenceId!.Value)
            .Distinct()
            .ToList();

        var salesById = (await _saleRepository.GetByIdsAsync(referenceIds))
            .ToDictionary(x => x.Id);

        var plansByDeliveryReferenceId =
            await _planRepository.GetByDeliveryReferenceIdsAsync(referenceIds);

        var customerIds = salesById.Values
            .Where(x => x.CustomerId.HasValue)
            .Select(x => x.CustomerId!.Value)
            .Concat(plansByDeliveryReferenceId.Values.Select(x => x.CustomerId))
            .Distinct()
            .ToList();

        var customersById = (await _customerRepository.GetByIdsAsync(
            customerIds,
            false))
            .ToDictionary(x => x.Id);

        var items = new List<StockMovementItemResponse>(movements.Count);

        foreach (var movement in movements.OrderByDescending(x => x.CreatedAt))
        {
            if (!products.TryGetValue(movement.ProductId, out var product))
                return Result<StockMovementReportResponse>.Fail(
                    Error.NotFound($"Product {movement.ProductId} not found"));

            if (!employees.TryGetValue(movement.CreatedBy, out var employee))
                return Result<StockMovementReportResponse>.Fail(
                    Error.NotFound(
                        $"Employee responsible for movement {movement.Id} not found"));

            var reference = ResolveReference(
                movement.ReferenceId,
                salesById,
                plansByDeliveryReferenceId);

            var customer = ResolveCustomer(
                movement.ReferenceId,
                salesById,
                plansByDeliveryReferenceId,
                customersById);

            items.Add(new StockMovementItemResponse
            {
                Id = movement.Id,
                Date = NormalizeUtc(movement.CreatedAt),
                Product = new ProductMovementResponse
                {
                    Id = product.Id,
                    Name = product.Name.Value
                },
                Type = movement.Type,
                Quantity = movement.Quantity,
                Reason = movement.Reason,
                Reference = reference,
                Employee = new EmployeeMovementResponse
                {
                    Id = employee.Id,
                    Name = employee.Name.Value
                },
                Customer = customer
            });
        }

        return Result<StockMovementReportResponse>.Success(
            new StockMovementReportResponse
            {
                Items = items
            });
    }

    private static StockMovementReferenceResponse? ResolveReference(
        Guid? referenceId,
        IReadOnlyDictionary<Guid, AquaGas.Sale.Domain.Models.Sale> salesById,
        IReadOnlyDictionary<Guid, AquaGas.Plan.Domain.Models.Plan> plansByDeliveryReferenceId)
    {
        if (!referenceId.HasValue)
            return null;

        if (salesById.ContainsKey(referenceId.Value))
        {
            return new StockMovementReferenceResponse
            {
                Id = referenceId.Value,
                Type = "SALE"
            };
        }

        if (plansByDeliveryReferenceId.TryGetValue(referenceId.Value, out var plan))
        {
            return new StockMovementReferenceResponse
            {
                Id = plan.Id,
                Type = "PLAN"
            };
        }

        return null;
    }

    private static CustomerMovementResponse? ResolveCustomer(
        Guid? referenceId,
        IReadOnlyDictionary<Guid, AquaGas.Sale.Domain.Models.Sale> salesById,
        IReadOnlyDictionary<Guid, AquaGas.Plan.Domain.Models.Plan> plansByDeliveryReferenceId,
        IReadOnlyDictionary<Guid, AquaGas.Customer.Domain.Models.Customer> customersById)
    {
        if (!referenceId.HasValue)
            return null;

        Guid? customerId = null;

        if (salesById.TryGetValue(referenceId.Value, out var sale))
            customerId = sale.CustomerId;
        else if (plansByDeliveryReferenceId.TryGetValue(referenceId.Value, out var plan))
            customerId = plan.CustomerId;

        if (!customerId.HasValue)
            return null;

        if (!customersById.TryGetValue(customerId.Value, out var customer))
            return null;

        return new CustomerMovementResponse
        {
            Id = customer.Id,
            Name = customer.Name.Value
        };
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
