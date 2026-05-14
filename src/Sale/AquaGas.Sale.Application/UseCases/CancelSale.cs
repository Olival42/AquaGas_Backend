using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Product.Application.Repositories;
using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.Models;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Domain.Models;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Sale.Application.UseCases;

public sealed class CancelSale : ICancelSale
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IUserContextService _userContextService;
    private readonly IAuditLogService _auditLogService;

    public CancelSale(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository,
        IUserContextService userContextService,
        IAuditLogService auditLogService)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
        _userContextService = userContextService;
        _auditLogService = auditLogService;
    }

    public async Task<Result<string>> Execute(
        Guid id,
        CancelSaleInput input)
    {
        var currentUser = _userContextService.GetUserId();
        var userName = _userContextService.GetUserName();

        if (currentUser.IsFailure)
            return Result<string>.Fail(currentUser.Errors.ToArray());

        if (userName.IsFailure)
            return Result<string>.Fail(userName.Errors.ToArray());

        var sale = await _saleRepository.GetByIdWithItemsAsync(id);

        if (sale is null)
            return Result<string>.Fail(
                Error.NotFound("Sale not found"));

        if (sale.Status == SaleStatus.Canceled)
            return Result<string>.Fail(
                Error.Conflict(
                    "Sale already canceled"));

        if (sale.Date < DateTime.UtcNow.AddHours(-24))
            return Result<string>.Fail(
                Error.Conflict(
                    "Cancellation period expired"));

        var oldValues = new
        {
            Status = sale.Status.ToString(),
            sale.CancelReason
        };

        foreach (var item in sale.Items)
        {
            var product =
                await _productRepository.GetByIdAsync(
                    item.ProductId, false);

            if (product is null)
                return Result<string>.Fail(
                    Error.NotFound(
                        $"Product {item.ProductId} not found"));

            product.IncreaseStock(item.Quantity.Value);

            var movement = StockMovement.Create(
                productId: product.Id,
                type: StockMovementType.Entry,
                quantity: item.Quantity.Value,
                reason: $"Sale canceled: {input.Reason}",
                createdBy: currentUser.Value,
                referenceId: sale.Id
            );

            await _stockMovementRepository.AddAsync(movement);
        }

        sale.Cancel(input.Reason);

        _saleRepository.Update(sale);

        await _productRepository.SaveChangesAsync();

        await _saleRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId: currentUser.Value,
            userName: userName.Value,
            action: AuditAction.UPDATE,
            entityType: "Sale",
            entityId: sale.Id,
            oldValues: oldValues,
            newValues: new
            {
                Status = sale.Status.ToString(),
                sale.CancelReason
            });

        return Result<string>.Success("Sale canceled successfully");
    }
}