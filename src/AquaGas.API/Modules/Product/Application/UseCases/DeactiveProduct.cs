using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Product.Domain.Repositories;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;

namespace AquaGas.Api.Modules.Product.Application.UseCases;

public class DeactiveProduct : IDeactiveProduct
{
    private readonly IProductRepository _productRepository;
    private readonly IUserContextService _userContextService;
    private readonly IAuditLogService _auditLogService;

    public DeactiveProduct(
    IProductRepository productRepository,
    IUserContextService userContextService,
    IAuditLogService auditLogService)
    {
        _productRepository = productRepository;
        _userContextService = userContextService;
        _auditLogService = auditLogService;
    }

    public async Task<Result<object>> Execute(Guid id)
    {
        var currentUser = _userContextService.GetUserId();

        var userName = _userContextService.GetUserName();

        if (currentUser.IsFailure)
            return Result<object>.Fail(currentUser.Errors.ToArray());

        if (userName.IsFailure)
            return Result<object>.Fail(userName.Errors.ToArray());

        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
            return Result<object>.Fail(Error.NotFound("Product not found"));

        var oldValues = new { product.Id, product.Quantity.Value, product.IsActive };

        var deactivationResult = product.Deactivate();
        if (deactivationResult.IsFailure)
            return Result<object>.Fail(deactivationResult.Errors.ToArray());

        await _auditLogService.LogAsync(
            userId: currentUser.Value,
            userName: userName.Value,
            action: AuditAction.DEACTIVATE,
            entityType: "Product",
            entityId: product.Id,
            oldValues: oldValues,
            newValues: new { product.Id, product.IsActive }
        );

        await _productRepository.SaveChangesAsync();

        return Result<object>.Success(null!);
    }
}