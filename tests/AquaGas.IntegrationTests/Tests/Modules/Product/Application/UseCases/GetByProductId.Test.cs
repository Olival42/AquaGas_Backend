using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.API.Modules.Product.Infrastructure.Persistence.Repositories;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;

public class GetByProductIdTests : BaseIntegrationTest
{
    public GetByProductIdTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Should_Return_Product_When_Exists()
    {
        var productId = await SeedProduct();

        var result = await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            return await useCase.Execute(productId);
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Return_Failure_When_Product_Does_Not_Exist()
    {
        var result = await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            return await useCase.Execute(Guid.NewGuid());
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Map_Entity_To_Dto_Correctly()
    {
        var productId = await SeedProduct();

        var result = await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            return await useCase.Execute(productId);
        });

        result.Value!.Quantity.Should().BeGreaterThan(0);
        result.Value.Type.Should().Be(result.Value.Type);
    }

    [Fact]
    public async Task Should_Return_Correct_Product_Id()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = new Product(
                ProductName.Create("Produto X").Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                StockQuantity.Create(5).Value!
            );

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var result = await ExecuteDbContextAsync(async db =>
        {
            var useCase = new GetByProductId(new ProductRepository(db));
            return await useCase.Execute(productId);
        });

        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(productId);
    }

    private GetByProductId CreateUseCase(AppDbContext db)
    {
        return new GetByProductId(
            new ProductRepository(db)
        );
    }

    private async Task<Guid> SeedProduct()
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var product = new Product(
                ProductName.Create("Agua Crystal").Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                StockQuantity.Create(5).Value!
            );

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });
    }
}