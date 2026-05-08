using ProductEntity = AquaGas.Api.Modules.Product.Domain.Models.Product;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Domain.Repositories;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using Mapster;
using AquaGas.Api.Modules.Product.Application.Dtos.Responses;
using AquaGas.API.Modules.Product.Application.Services;
using AquaGas.API.Modules.Product.Application.Repositories;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;

namespace AquaGas.Api.Modules.Product.Application.UseCases;

public class UpdateStock : IUpdateStock
{
    private readonly IProductRepository _productRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContextService _userContextService;
    private readonly IStockMovementRepository _stockMovementRepository;

    public UpdateStock(
        IProductRepository productRepository,
        IAuditLogService auditLogService,
        IUserContextService userContextService,
        IStockMovementRepository stockMovementRepository)
    {
        _productRepository = productRepository;
        _auditLogService = auditLogService;
        _userContextService = userContextService;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<Result<ProductResponse>> Execute(UpdateStockInput data, Guid id)
    {
        var validation = UpdateStockValidationFactory.Combine(data);
        if (validation.IsFailure)
            return Result<ProductResponse>.Fail(validation.Errors.ToArray());

        var currentUser = _userContextService.GetUserId();
        var userName = _userContextService.GetUserName();

        if (currentUser.IsFailure)
            return Result<ProductResponse>.Fail(currentUser.Errors.ToArray());

        if (userName.IsFailure)
            return Result<ProductResponse>.Fail(userName.Errors.ToArray());

        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
            return Result<ProductResponse>.Fail(
                Error.NotFound("Product not found"));

        var v = validation.Value!;

        var oldValues = BuildSnapshot(product);

        var resultStockMovement = v.StockMovementType switch
        {
            StockMovementType.Entry => product.IncreaseStock(v.Quantity.Value),
            StockMovementType.Exit => product.DecreaseStock(v.Quantity.Value),
            _ => Result.Fail(Error.Validation("Type of movement stock is invalid. Allowed: Entry, Exit", "StockMovementType"))
        };

        if (resultStockMovement.IsFailure)
            return Result<ProductResponse>.Fail(resultStockMovement.Errors.ToArray());

        var stockMovement = StockMovement.Create(
            productId: id,
            type :v.StockMovementType,
            quantity: v.Quantity.Value,
            reason: v.Reason,
            createdBy: currentUser.Value
        );

        await _stockMovementRepository.AddAsync(stockMovement);

        var newValues = BuildSnapshot(product);

        await _auditLogService.LogAsync(
            userId: currentUser.Value,
            userName: userName.Value,
            action: AuditAction.UPDATE,
            entityType: "Product",
            entityId: product.Id,
            oldValues: oldValues,
            newValues: newValues
        );

        await _productRepository.SaveChangesAsync();

        return Result<ProductResponse>.Success(
            product.Adapt<ProductResponse>()
        );
    }

    private static object BuildSnapshot(ProductEntity c)
    {
        return new
        {
            Name = c.Name.Value,
            Quantity = c.Quantity.Value
        };
    }
}