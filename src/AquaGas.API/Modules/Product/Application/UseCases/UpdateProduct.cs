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

namespace AquaGas.Api.Modules.Product.Application.UseCases;

public class UpdateProduct : IUpdateProduct
{
    private readonly IProductRepository _productRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContextService _userContextService;

    public UpdateProduct(
        IProductRepository productRepository,
        IAuditLogService auditLogService,
        IUserContextService userContextService)
    {
        _productRepository = productRepository;
        _auditLogService = auditLogService;
        _userContextService = userContextService;
    }

    public async Task<Result<ProductResponse>> Execute(UpdateProductInput data, Guid id)
    {
        var validation = UpdateProductValidationFactory.Combine(data);
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

        if (v.Name is not null)
        {
            var exists = await _productRepository.AnyByNameAsync(
                v.Name.Value,
                product.Id
            );

            if (exists)
                return Result<ProductResponse>.Fail(
                Error.Conflict("Product with this name already exists")
            );
        }

        if (v.Type is not null && product.Quantity.Value > 0)
        {
            return Result<ProductResponse>.Fail(
                Error.Conflict("Cannot change product type while stock exists")
            );
        }

        var oldValues = BuildSnapshot(product);

        product.Update(v.Name, v.Type, v.Price, null);

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
            Type = c.Type,
            Price = c.Price.Value,
        };
    }
}