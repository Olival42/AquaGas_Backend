using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.Dtos.Responses;
using AquaGas.Sale.Application.Services;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using AquaGas.Product.Application.Repositories;
using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.Models;

using Microsoft.EntityFrameworkCore;

using ProductEntity = AquaGas.Product.Domain.Models.Product;
using SaleEntity = AquaGas.Sale.Domain.Models.Sale;
using SaleItemEntity = AquaGas.Sale.Domain.Models.SaleItem;

namespace AquaGas.Sale.Application.UseCases;

public sealed class RegisterSale : IRegisterSale
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IStockMovementRepository _stockMovementRepository;

    public RegisterSale(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IUserContextService userContextService,
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        ICustomerRepository customerRepository,
        IAuditLogService auditLogService,
        IStockMovementRepository stockMovementRepository)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _userContextService = userContextService;
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _customerRepository = customerRepository;
        _auditLogService = auditLogService;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<Result<SaleResponse>> Execute(
        RegisterSaleInput input)
    {
        var currentUserId = _userContextService.GetUserId();

        if (currentUserId.IsFailure)
            return Result<SaleResponse>.Fail(
                currentUserId.Errors.ToArray());

        var currentUserName = _userContextService.GetUserName();

        if (currentUserName.IsFailure)
            return Result<SaleResponse>.Fail(
                currentUserName.Errors.ToArray());

        var currentUserRole = _userContextService.GetRole();

        if (currentUserRole.IsFailure)
            return Result<SaleResponse>.Fail(
                currentUserRole.Errors.ToArray());

        var user = await _userRepository.GetByIdAsync(currentUserId.Value);

        if (user is null)
            return Result<SaleResponse>.Fail(
                Error.Unauthorized(
                    "Authenticated user was not found"));

        var employeeId = user.EmployeeId;

        var validation = RegisterSaleValidationFactory.Combine(input);

        if (validation.IsFailure)
            return Result<SaleResponse>.Fail(
                validation.Errors.ToArray());

        var validated = validation.Value!;

        if (validated.Discount is not null &&
            currentUserRole.Value == Role.Employee)
        {
            return Result<SaleResponse>.Fail(
                Error.Forbidden(
                    "Employees cannot apply discounts; only managers can"));
        }

        if (validated.CustomerId is { } customerId)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId);

            if (customer is null)
                return Result<SaleResponse>.Fail(
                    Error.NotFound("Customer not found"));
        }

        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (employee is null)
            return Result<SaleResponse>.Fail(
                Error.NotFound("Employee linked to user was not found"));

        var productsToPersist = new Dictionary<Guid, ProductEntity>();
        var lineContexts = new List<SaleLineContext>();

        foreach (var item in validated.SaleItems)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);

            if (product is null)
                return Result<SaleResponse>.Fail(
                    Error.NotFound(
                        $"Product {item.ProductId} was not found"));

            var lineTotalDecimal = decimal.Round(
                (decimal)item.Quantity.Value * product.Price.Value,
                2,
                MidpointRounding.AwayFromZero);

            var linePriceResult = Price.Create(lineTotalDecimal);

            if (linePriceResult.IsFailure)
                return Result<SaleResponse>.Fail(linePriceResult.Errors.ToArray());

            var decreaseResult = product.DecreaseStock(item.Quantity.Value);

            if (decreaseResult.IsFailure)
            {
                var mapped = decreaseResult.Errors
                    .Select(e => e.Code == "INSUFFICIENT_STOCK"
                        ? Error.InsufficientStockForProduct(
                            product.Name.Value,
                            product.Id,
                            product.Quantity.Value,
                            item.Quantity.Value)
                        : e)
                    .ToArray();

                return Result<SaleResponse>.Fail(mapped);
            }

            productsToPersist[product.Id] = product;

            lineContexts.Add(new SaleLineContext(
                product,
                item.Quantity,
                linePriceResult.Value!));
        }

        decimal subtotal = decimal.Round(
            lineContexts.Sum(x => x.LineTotal.Value),
            2,
            MidpointRounding.AwayFromZero);

        var discountPercentage = validated.Discount?.Value ?? 0;

        var discountValue = decimal.Round(
            subtotal * ((decimal)discountPercentage / 100m),
            2,
            MidpointRounding.AwayFromZero);

        var finalTotal = decimal.Round(
            subtotal - discountValue,
            2,
            MidpointRounding.AwayFromZero);

        var totalResult = Price.Create(finalTotal);

        if (totalResult.IsFailure)
            return Result<SaleResponse>.Fail(
                totalResult.Errors.ToArray());

        var sale = new SaleEntity(
            validated.CustomerId,
            employeeId,
            totalResult.Value!,
            validated.Discount);

        foreach (var line in lineContexts)
        {
            var stockMovement = StockMovement.Create(
                productId: line.Product.Id,
                type: StockMovementType.Exit,
                quantity: line.Quantity.Value,
                reason: "Sale completed",
                createdBy: currentUserId.Value,
                referenceId: sale.Id
            );

            await _stockMovementRepository.AddAsync(stockMovement);
        }

        foreach (var line in lineContexts)
        {
            var saleItem = new SaleItemEntity(
                sale.Id,
                line.Product.Id,
                line.Quantity,
                line.LineTotal);

            sale.AddItem(saleItem);
        }

        try
        {
            foreach (var product in productsToPersist.Values)
                _productRepository.Update(product);

            await _productRepository.SaveChangesAsync();

            await _saleRepository.AddAsync(sale);

            await _saleRepository.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<SaleResponse>.Fail(
                Error.Conflict(
                    "Stock concurrency conflict: data was modified by another operation"));
        }

        await _auditLogService.LogAsync(
            userId: currentUserId.Value,
            userName: currentUserName.Value,
            action: AuditAction.CREATE,
            entityType: "Sale",
            entityId: sale.Id,
            oldValues: null,
            newValues: new
            {
                sale.Id,
                sale.CustomerId,
                sale.EmployeeId,
                Total = sale.Total.Value
            });

        CustomerSaleResponse? customerResponse = null;

        if (validated.CustomerId is { } cid)
        {
            var customerEntity =
                await _customerRepository.GetByIdAsync(cid);

            if (customerEntity is not null)
            {
                customerResponse = new CustomerSaleResponse
                {
                    Id = customerEntity.Id,
                    Name = customerEntity.Name.Value,
                    Document = customerEntity.Document.Value
                };
            }
        }

        var productNameById = lineContexts
            .GroupBy(c => c.Product.Id)
            .ToDictionary(
                g => g.Key,
                g => g.First().Product.Name.Value);

        var itemResponses = sale.Items
            .Select(x => new SaleItemResponse
            {
                ProductId = x.ProductId,
                ProductName = productNameById[x.ProductId],
                Quantity = x.Quantity.Value,
                Total = x.TotalPrice.Value
            })
            .ToList();

        var response = new SaleResponse
        {
            Id = sale.Id,
            Customer = customerResponse,
            Employee = new EmployeeSaleResponse
            {
                Id = employee.Id,
                Name = employee.Name.Value
            },
            Items = itemResponses,
            Subtotal = subtotal,
            Discount = discountPercentage,
            Total = sale.Total.Value,
            Status = sale.Status,
            CancelReason = sale.CancelReason!, 
            CreatedAt = sale.Date
        };

        return Result<SaleResponse>.Success(response);
    }

    private sealed record SaleLineContext(
        ProductEntity Product,
        StockQuantity Quantity,
        Price LineTotal);
}
