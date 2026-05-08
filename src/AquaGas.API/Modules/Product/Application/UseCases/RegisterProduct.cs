using ProductEntity = AquaGas.Api.Modules.Product.Domain.Models.Product;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Application.Services;
using Mapster;
using AquaGas.Api.Shared.Errors;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Application.Dtos.Responses;
using AquaGas.API.Modules.Product.Application.Services;
using AquaGas.Api.Modules.Product.Application.Dtos.Resposes;

namespace AquaGas.Api.Modules.Product.Application.UseCases;

public class RegisterProduct : IRegisterProduct
{
    private readonly IProductRepository _repository;
    private readonly IAuditLogService _audit;
    private readonly IUserContextService _userContext;

    public RegisterProduct(
        IProductRepository repository,
        IAuditLogService audit,
        IUserContextService userContext)
    {
        _repository = repository;
        _audit = audit;
        _userContext = userContext;
    }

    public async Task<Result<ProductResponse>> Execute(RegisterProductInput data)
    {
        var validation = RegisterProductValidationFactory.Combine(data);
        if (validation.IsFailure)
            return Result<ProductResponse>.Fail(validation.Errors.ToArray());

        var userId = _userContext.GetUserId();
        var userName = _userContext.GetUserName();

        if (userId.IsFailure)
            return Result<ProductResponse>.Fail(userId.Errors.ToArray());

        if (userName.IsFailure)
            return Result<ProductResponse>.Fail(userName.Errors.ToArray());

        var v = validation.Value!;

        var product = await _repository.GetByNameAsync(v.Name.Value);

        if (product is null)
            return await Create(v, userId.Value, userName.Value!);

        if (product.IsActive)
        {
            return Result<ProductResponse>.Fail(
                Error.Conflict("Product with this name already exists")
            );
        }

        return await Reactivate(product, v, userId.Value, userName.Value!);
    }

    private async Task<Result<ProductResponse>> Create(
        RegisterProductValidated v,
        Guid userId,
        string userName)
    {
        var product = v.Adapt<ProductEntity>();

        await _repository.AddAsync(product);

        await Log(userId, userName, product, null, AuditAction.CREATE);

        await _repository.SaveChangesAsync();

        return Result<ProductResponse>.Success(product.Adapt<ProductResponse>());
    }

    private async Task<Result<ProductResponse>> Reactivate(
        ProductEntity product,
        RegisterProductValidated v,
        Guid userId,
        string userName)
    {
        var oldSnapshot = BuildSnapshot(product);

        product.Update(v.Name, v.Type, v.Price, v.Quantity);
        product.Reactivate();

        await Log(userId, userName, product, oldSnapshot, AuditAction.UPDATE);

        await _repository.SaveChangesAsync();

        return Result<ProductResponse>.Success(product.Adapt<ProductResponse>());
    }

    private async Task Log(
        Guid userId,
        string userName,
        ProductEntity product,
        object? oldValues,
        AuditAction action)
    {
        await _audit.LogAsync(
            userId,
            userName,
            action,
            "Product",
            product.Id,
            oldValues,
            BuildSnapshot(product)
        );
    }

    private static object BuildSnapshot(ProductEntity c)
    {
        return new
        {
            Name = c.Name.Value,
            Type = c.Type,
            Quantity = c.Quantity.Value,
            Price = c.Price.Value,
            Active = c.IsActive,
        };
    }
}