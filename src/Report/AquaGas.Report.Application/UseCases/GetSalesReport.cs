using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Report.Application.Configuration;
using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.Dtos.Responses;
using AquaGas.Sale.Domain.Models;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Report.Application.UseCases;

public sealed class GetSalesReport
    : IGetSalesReport
{
    private readonly IUserContextService _userContextService;
    private readonly ISaleRepository _saleRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IProductRepository _productRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IReportSettings _reportSettings;

    public GetSalesReport(
        IUserContextService userContextService,
        ISaleRepository saleRepository,
        IDeliveryRepository deliveryRepository,
        IPlanRepository planRepository,
        IBillingRepository billingRepository,
        IProductRepository productRepository,
        IEmployeeRepository employeeRepository,
        ICustomerRepository customerRepository,
        IReportSettings reportSettings)
    {
        _userContextService = userContextService;
        _saleRepository = saleRepository;
        _deliveryRepository = deliveryRepository;
        _planRepository = planRepository;
        _billingRepository = billingRepository;
        _productRepository = productRepository;
        _employeeRepository = employeeRepository;
        _customerRepository = customerRepository;
        _reportSettings = reportSettings;
    }

    public async Task<Result<SalesReportResponse>> Execute(
        SalesReportInput input)
    {
        var currentUserRole = _userContextService.GetRole();

        if (currentUserRole.IsFailure)
            return Result<SalesReportResponse>.Fail(currentUserRole.Errors.ToArray());

        if (currentUserRole.Value != Role.Manager)
            return Result<SalesReportResponse>.Fail(
                Error.Forbidden("Only managers can access sales reports"));

        var startUtc = NormalizeUtc(input.Start);
        var endUtc = NormalizeUtc(input.End);

        if (startUtc > endUtc)
            return Result<SalesReportResponse>.Fail(
                Error.Validation(
                    "Start must be less than or equal to End",
                    "Period"));

        if ((endUtc - startUtc).TotalDays > _reportSettings.MaxSalesReportIntervalDays)
            return Result<SalesReportResponse>.Fail(
                Error.Validation(
                    $"The maximum allowed range is {_reportSettings.MaxSalesReportIntervalDays} days",
                    "Period"));

        var sales = (await _saleRepository.GetByPeriodAsync(startUtc, endUtc))
            .OrderByDescending(x => x.Date)
            .ToList();

        var deliveredDeliveries = await _deliveryRepository.GetDeliveredByPeriodAsync(startUtc, endUtc);
        var plans = await _planRepository.GetByIdsAsync(deliveredDeliveries.Select(x => x.PlanId));
        var plansById = plans.ToDictionary(x => x.Id);
        var billings = await _billingRepository.GetByPlanIdsAsync(plansById.Keys);
        var billingsByPlanAndPeriod = billings.ToDictionary(x => (x.PlanId, x.Period));

        var productIds = sales
            .SelectMany(x => x.Items.Select(i => i.ProductId))
            .Concat(plans.SelectMany(x => x.Items.Select(i => i.ProductId)))
            .Distinct()
            .ToList();

        var productsById = (await _productRepository.GetByIdsAsync(productIds, false))
            .ToDictionary(x => x.Id);

        var employeeIds = sales
            .Select(x => x.EmployeeId)
            .Concat(plans.Select(x => x.EmployeeId))
            .Distinct()
            .ToList();

        var employeesById = (await _employeeRepository.GetByIdsAsync(employeeIds, false))
            .ToDictionary(x => x.Id);

        var customerIds = sales
            .Where(x => x.CustomerId.HasValue)
            .Select(x => x.CustomerId!.Value)
            .Concat(plans.Select(x => x.CustomerId))
            .Distinct()
            .ToList();

        var customersById = (await _customerRepository.GetByIdsAsync(customerIds, false))
            .ToDictionary(x => x.Id);

        var items = new List<SalesReportItemResponse>();

        foreach (var sale in sales)
        {
            if (!employeesById.TryGetValue(sale.EmployeeId, out var employee))
                return Result<SalesReportResponse>.Fail(
                    Error.NotFound($"Employee {sale.EmployeeId} not found"));

            items.Add(new SalesReportItemResponse
            {
                Id = $"sale-{sale.Id}",
                Date = NormalizeUtc(sale.Date),
                Customer = sale.CustomerId.HasValue &&
                           customersById.TryGetValue(sale.CustomerId.Value, out var customer)
                    ? new CustomerSalesResponse
                    {
                        Id = customer.Id,
                        Name = customer.Name.Value
                    }
                    : null,
                Employee = new EmployeeSalesResponse
                {
                    Id = employee.Id,
                    Name = employee.Name.Value
                },
                Type = "SALE",
                Status = MapSaleStatus(sale.Status),
                ItemsCount = sale.Items.Count,
                Total = sale.Total.Value,
                Items = sale.Items
                    .Select(item => MapSpotSaleItem(item, productsById))
                    .ToList()
            });
        }

        foreach (var delivery in deliveredDeliveries)
        {
            if (!plansById.TryGetValue(delivery.PlanId, out var plan))
                return Result<SalesReportResponse>.Fail(
                    Error.NotFound($"Plan {delivery.PlanId} not found"));

            if (!employeesById.TryGetValue(plan.EmployeeId, out var employee))
                return Result<SalesReportResponse>.Fail(
                    Error.NotFound($"Employee {plan.EmployeeId} not found"));

            if (!billingsByPlanAndPeriod.TryGetValue((plan.Id, delivery.Period), out var billing))
                return Result<SalesReportResponse>.Fail(
                    Error.NotFound(
                        $"Billing for plan {plan.Id} period {delivery.Period} not found"));

            var contractItems = BuildContractItems(plan, billing.Amount.Value, productsById);

            items.Add(new SalesReportItemResponse
            {
                Id = $"plan-{delivery.Id}",
                Date = NormalizeUtc(delivery.DeliveryDate ?? delivery.DueDate),
                Customer = customersById.TryGetValue(plan.CustomerId, out var customer)
                    ? new CustomerSalesResponse
                    {
                        Id = customer.Id,
                        Name = customer.Name.Value
                    }
                    : null,
                Employee = new EmployeeSalesResponse
                {
                    Id = employee.Id,
                    Name = employee.Name.Value
                },
                Type = "PLAN",
                Status = "FINISHED",
                ItemsCount = plan.Items.Count,
                Total = billing.Amount.Value,
                Items = contractItems
            });
        }

        items = items
            .OrderByDescending(x => x.Date)
            .ToList();

        var finishedItems = items
            .Where(x => x.Status == "FINISHED")
            .ToList();

        var cancelledSales = items.Count(x => x.Status == "CANCELLED");
        var totalSpotSales = finishedItems
            .Where(x => x.Type == "SALE")
            .Sum(x => x.Total);
        var totalContractSales = finishedItems
            .Where(x => x.Type == "PLAN")
            .Sum(x => x.Total);
        var totalRevenue = totalSpotSales + totalContractSales;
        var totalSales = finishedItems.Count;

        return Result<SalesReportResponse>.Success(
            new SalesReportResponse
            {
                Summary = new SalesSummaryResponse
                {
                    TotalSpotSales = totalSpotSales,
                    TotalContractSales = totalContractSales,
                    TotalRevenue = totalRevenue,
                    TotalSales = totalSales,
                    CancelledSales = cancelledSales,
                    AverageTicket = totalSales == 0
                        ? 0m
                        : decimal.Round(
                            totalRevenue / totalSales,
                            2,
                            MidpointRounding.AwayFromZero),
                    Period = new SalesPeriodResponse
                    {
                        Start = startUtc,
                        End = endUtc
                    }
                },
                Items = items
            });
    }

    private static SalesProductItemResponse MapSpotSaleItem(
        SaleItem item,
        IReadOnlyDictionary<Guid, AquaGas.Product.Domain.Models.Product> productsById)
    {
        productsById.TryGetValue(item.ProductId, out var product);

        var subtotal = item.TotalPrice.Value;
        var unitPrice = item.Quantity.Value == 0
            ? 0m
            : decimal.Round(
                subtotal / item.Quantity.Value,
                2,
                MidpointRounding.AwayFromZero);

        return new SalesProductItemResponse
        {
            ProductId = item.ProductId,
            ProductName = product?.Name.Value ?? "Unknown",
            Quantity = item.Quantity.Value,
            UnitPrice = unitPrice,
            Subtotal = subtotal
        };
    }

    private static List<SalesProductItemResponse> BuildContractItems(
        PlanEntity plan,
        decimal contractTotal,
        IReadOnlyDictionary<Guid, AquaGas.Product.Domain.Models.Product> productsById)
    {
        var bases = plan.Items
            .Select(item =>
            {
                productsById.TryGetValue(item.ProductId, out var product);
                var baseValue = (product?.Price.Value ?? 0m) * item.Quantity.Value;

                return new
                {
                    Item = item,
                    ProductName = product?.Name.Value ?? "Unknown",
                    BaseValue = baseValue
                };
            })
            .ToList();

        var baseTotal = bases.Sum(x => x.BaseValue);
        var allocated = new List<SalesProductItemResponse>(bases.Count);
        decimal runningTotal = 0m;

        for (var index = 0; index < bases.Count; index++)
        {
            var current = bases[index];

            decimal subtotal;

            if (index == bases.Count - 1)
            {
                subtotal = decimal.Round(
                    contractTotal - runningTotal,
                    2,
                    MidpointRounding.AwayFromZero);
            }
            else if (baseTotal == 0m)
            {
                subtotal = decimal.Round(
                    contractTotal / bases.Count,
                    2,
                    MidpointRounding.AwayFromZero);
            }
            else
            {
                subtotal = decimal.Round(
                    contractTotal * (current.BaseValue / baseTotal),
                    2,
                    MidpointRounding.AwayFromZero);
            }

            runningTotal += subtotal;

            allocated.Add(new SalesProductItemResponse
            {
                ProductId = current.Item.ProductId,
                ProductName = current.ProductName,
                Quantity = current.Item.Quantity.Value,
                UnitPrice = current.Item.Quantity.Value == 0
                    ? 0m
                    : decimal.Round(
                        subtotal / current.Item.Quantity.Value,
                        2,
                        MidpointRounding.AwayFromZero),
                Subtotal = subtotal
            });
        }

        return allocated;
    }

    private static string MapSaleStatus(SaleStatus status)
        => status == SaleStatus.Canceled ? "CANCELLED" : "FINISHED";

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
