using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Application.Dtos.Responses;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using SaleEntity = AquaGas.Sale.Domain.Models.Sale;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using AquaGas.Sale.Domain.Models;

namespace AquaGas.Customer.Application.UseCases;

public sealed class CustomerConsumptionHistory
    : ICustomerConsumptionHistory
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IProductRepository _productRepository;

    public CustomerConsumptionHistory(
        ICustomerRepository customerRepository,
        ISaleRepository saleRepository,
        IPlanRepository planRepository,
        IDeliveryRepository deliveryRepository,
        IProductRepository productRepository)
    {
        _customerRepository = customerRepository;
        _saleRepository = saleRepository;
        _planRepository = planRepository;
        _deliveryRepository = deliveryRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<CustomerConsumptionResponse>> Execute(
        Guid id,
        CustomerConsumptionHistoryInput input)
    {
        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer is null)
            return Result<CustomerConsumptionResponse>.Fail(
                Error.NotFound("Customer not found"));

        var sales =
            (await _saleRepository.GetByCustomerIdWithItemsAsync(id))
            .ToList();

        var plans =
            await _planRepository.GetByCustomerIdAsync(id);

        var planById = plans.ToDictionary(x => x.Id);

        var deliveries =
            await _deliveryRepository.GetByPlanIdsAsync(planById.Keys);

        var deliveryCountByPlanId = deliveries
            .GroupBy(x => x.PlanId)
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        var productNames = await BuildProductNameMapAsync(sales, plans);

        var salesItems = sales
            .Where(sale =>
                IsWithinPeriod(sale.Date, input.StartDate, input.EndDate) &&
                sale.Status == SaleStatus.Finished)
            .Select(sale => MapSale(sale, productNames))
            .ToList();

        var deliveryItems = deliveries
            .Where(delivery =>
                planById.ContainsKey(delivery.PlanId) &&
                IsWithinPeriod(
                    delivery.DeliveryDate ?? delivery.DueDate,
                    input.StartDate,
                    input.EndDate) &&
                delivery.Status == DeliveryStatus.Delivered)
            .Select(delivery => MapDelivery(
                delivery,
                planById[delivery.PlanId],
                deliveryCountByPlanId,
                productNames))
            .ToList();

        var mergedItems = salesItems
            .Concat(deliveryItems)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Type)
            .ToList();

        var summary = BuildSummary(
            salesItems,
            deliveryItems,
            input.StartDate,
            input.EndDate);

        var response = new CustomerConsumptionResponse
        {
            Summary = summary,
            Items = mergedItems
        };

        return Result<CustomerConsumptionResponse>.Success(response);
    }

    private async Task<Dictionary<Guid, string>> BuildProductNameMapAsync(
        IEnumerable<SaleEntity> sales,
        IEnumerable<PlanEntity> plans)
    {
        var productIds = sales
            .SelectMany(x => x.Items)
            .Select(x => x.ProductId)
            .Concat(
                plans.SelectMany(x => x.Items)
                    .Select(x => x.ProductId))
            .Distinct()
            .ToList();

        var productNames = new Dictionary<Guid, string>();

        foreach (var productId in productIds)
        {
            var product = await _productRepository.GetByIdAsync(productId, false);
            productNames[productId] = product?.Name.Value ?? "Unknown";
        }

        return productNames;
    }

    private static CustomerConsumptionItemResponse MapSale(
        SaleEntity sale,
        IReadOnlyDictionary<Guid, string> productNames)
    {
        return new CustomerConsumptionItemResponse
        {
            OriginId = sale.Id.ToString(),
            Date = NormalizeDate(sale.Date),
            Type = "Sale",
            PlanId = null,
            Products = sale.Items
                .Select(item => new CustomerConsumptionProductResponse
                {
                    ProductId = item.ProductId,
                    Name = productNames.GetValueOrDefault(item.ProductId, "Unknown")
                })
                .ToList(),
            Quantity = sale.Items.Sum(x => x.Quantity.Value),
            Value = sale.Total.Value
        };
    }

    private static CustomerConsumptionItemResponse MapDelivery(
        Delivery delivery,
        PlanEntity plan,
        IReadOnlyDictionary<Guid, int> deliveryCountByPlanId,
        IReadOnlyDictionary<Guid, string> productNames)
    {
        var deliveryCount = deliveryCountByPlanId.GetValueOrDefault(plan.Id, 1);
        var allocatedValue = deliveryCount == 0
            ? 0m
            : decimal.Round(
                plan.Total.Value / deliveryCount,
                2,
                MidpointRounding.AwayFromZero);

        return new CustomerConsumptionItemResponse
        {
            OriginId = delivery.Id.ToString(),
            Date = NormalizeDate(delivery.DeliveryDate ?? delivery.DueDate),
            Type = "Delivery",
            PlanId = plan.Id,
            Products = plan.Items
                .Select(item => new CustomerConsumptionProductResponse
                {
                    ProductId = item.ProductId,
                    Name = productNames.GetValueOrDefault(item.ProductId, "Unknown")
                })
                .ToList(),
            Quantity = plan.Items.Sum(x => x.Quantity.Value),
            Value = allocatedValue
        };
    }

    private static CustomerConsumptionSummaryResponse BuildSummary(
        IEnumerable<CustomerConsumptionItemResponse> salesItems,
        IEnumerable<CustomerConsumptionItemResponse> deliveryItems,
        DateTime startDate,
        DateTime endDate)
    {
        var saleList = salesItems.ToList();

        var deliveriesList = deliveryItems.ToList();

        var totalSpent = decimal.Round(
            saleList.Sum(x => x.Value) + deliveriesList.Sum(x => x.Value),
            2,
            MidpointRounding.AwayFromZero);

        var totalOperations = saleList.Count + deliveriesList.Count;

        return new CustomerConsumptionSummaryResponse
        {
            TotalSales = saleList.Count,
            TotalDeliveries = deliveriesList.Count,
            TotalSpent = totalSpent,
            TotalItems = saleList.Sum(x => x.Quantity) + deliveriesList.Sum(x => x.Quantity),
            AverageTicket = totalOperations == 0
                ? 0m
                : decimal.Round(
                    totalSpent / totalOperations,
                    2,
                    MidpointRounding.AwayFromZero),
            Period = new CustomerConsumptionPeriodResponse
            {
                StartDate = NormalizeDate(startDate),
                EndDate = NormalizeDate(endDate)
            }
        };
    }

    private static bool IsWithinPeriod(
        DateTime value,
        DateTime startDate,
        DateTime endDate)
    {
        var normalizedValue = NormalizeDate(value);
        var normalizedStart = NormalizeDate(startDate);
        var normalizedEnd = NormalizeDate(endDate);

        return normalizedValue >= normalizedStart &&
            normalizedValue <= normalizedEnd;
    }

    private static DateTime NormalizeDate(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
