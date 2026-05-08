using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Modules.Product.Infrastructure.Persistence.Repositories;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.DependencyInjection;

public class DeactiveProductTests : BaseIntegrationTest
{
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly Mock<IUserContextService> _userMock = new();

    public DeactiveProductTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
        _userMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("test-user"));
    }

    [Fact]
    public async Task Should_Deactivate_Product_Successfully()
    {
        _userMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));
        var productId = await SeedProduct(active: true);

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var result = await useCase.Execute(productId);

            result.IsSuccess.Should().BeTrue();
        });

        await ExecuteDbContextAsync(async db =>
        {
            var product = await db.Products.FirstAsync(x => x.Id == productId);

            product.IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Should_Return_Failure_When_Product_Not_Found()
    {
        _userMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));
        var result = await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            return await useCase.Execute(Guid.NewGuid());
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Call_AuditLog_On_Deactivation()
    {
        _userMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));
        var productId = await SeedProduct();

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            await useCase.Execute(productId);
        });

        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            AuditAction.DEACTIVATE,
            "Product",
            It.IsAny<Guid>(),
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_If_Product_Has_Stock()
    {
        _userMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));
        var productId = await SeedProduct(quantity: 10);

        var result = await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);
            return await useCase.Execute(productId);
        });

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == "Product still in stock");
    }

    private DeactiveProduct CreateUseCase(AppDbContext db)
    {
        return new DeactiveProduct(
            new ProductRepository(db),
            _userMock.Object,
            _auditMock.Object
        );
    }

    private async Task<Guid> SeedProduct(bool active = true, int quantity = 0)
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var product = new Product(
                ProductName.Create("Produto Teste").Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                StockQuantity.Create(quantity).Value!
            );

            if (!active)
                product.Deactivate();

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });
    }
}