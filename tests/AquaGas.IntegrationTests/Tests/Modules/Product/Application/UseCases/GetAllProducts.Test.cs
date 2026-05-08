using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.Api.Modules.Product.Infrastructure.Persistence.Repositories;
using AquaGas.API.Modules.Product.Infrastructure.Persistence.Repositories;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;

public class GetAllProductsTests : BaseIntegrationTest
{
    public GetAllProductsTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Should_Return_Only_Active_Products()
    {
        await SeedProducts();

        var result = await ExecuteDbContextAsync(async db =>
        {
            var useCase = new GetAllProducts(new ProductRepository(db));

            return await useCase.Execute();
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value!.All(x => x != null).Should().BeTrue();
    }

    [Fact]
    public async Task Should_Not_Return_Inactive_Products()
    {
        await SeedProducts();

        var result = await ExecuteDbContextAsync(async db =>
        {
            var useCase = new GetAllProducts(new ProductRepository(db));
            return await useCase.Execute();
        });

        result.Value!.Any(x => x.Name == "INATIVO").Should().BeFalse();
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Active_Products()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var inactive = new Product(
                ProductName.Create("Produto Inativo").Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                StockQuantity.Create(0).Value!
            );

            inactive.Deactivate();

            db.Products.Add(inactive);
            await db.SaveChangesAsync();
        });

        var result = await ExecuteDbContextAsync(async db =>
        {
            var useCase = new GetAllProducts(new ProductRepository(db));
            return await useCase.Execute();
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Map_Entities_To_Response_Correctly()
    {
        await SeedProducts();

        var result = await ExecuteDbContextAsync(async db =>
        {
            var useCase = new GetAllProducts(new ProductRepository(db));
            return await useCase.Execute();
        });

        result.Value!.First().Should().NotBeNull();
        result.Value!.First().Name.Should().NotBeNullOrEmpty();
    }

    private async Task SeedProducts()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var active = new Product(
                ProductName.Create("Ativo").Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                StockQuantity.Create(5).Value!
            );

            var inactive = new Product(
                ProductName.Create("Inativo").Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                StockQuantity.Create(0).Value!
            );

            inactive.Deactivate();

            db.Products.AddRange(active, inactive);
            await db.SaveChangesAsync();
        });
    }
}