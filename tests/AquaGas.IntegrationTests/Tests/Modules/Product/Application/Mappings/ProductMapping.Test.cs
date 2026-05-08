using AquaGas.Api.Modules.Product.Application.Dtos.Responses;
using AquaGas.Api.Modules.Product.Application.Dtos.Resposes;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.API.Modules.Product.Application.Mappings;
using FluentAssertions;
using Mapster;
using Xunit;

namespace AquaGas.IntegrationTests.Tests.Mappings;

public class ProductMappingTests
{
    public ProductMappingTests()
    {
        ProductMapping.Register();
    }

    [Fact]
    public void Should_Map_Product_To_ProductResponse_Correctly()
    {
        var product = new Product(
            ProductName.Create("Agua Crystal").Value!,
            TypeProduct.Water,
            Price.Create(10).Value!,
            StockQuantity.Create(5).Value!
        );

        var dto = product.Adapt<ProductResponse>();

        dto.Should().NotBeNull();
        dto.Id.Should().Be(product.Id);
        dto.Name.Should().Be("Agua Crystal");
        dto.Price.Should().Be(10);
        dto.Quantity.Should().Be(5);
        dto.Type.Should().Be(TypeProduct.Water);
    }

    [Fact]
    public void Should_Map_RegisterProductValidated_To_ProductEntity()
    {
        var input = new RegisterProductValidated
        {
            Name = ProductName.Create("Agua Test").Value!,
            Type = TypeProduct.Water,
            Price = Price.Create(15).Value!,
            Quantity = StockQuantity.Create(3).Value!
        };

        var entity = input.Adapt<Product>();

        entity.Should().NotBeNull();
        entity.Name.Value.Should().Be("Agua Test");
        entity.Price.Value.Should().Be(15);
        entity.Quantity.Value.Should().Be(3);
        entity.Type.Should().Be(TypeProduct.Water);
    }

    [Fact]
    public void Should_Preserve_ValueObject_Data_When_Mapping()
    {
        var product = new Product(
            ProductName.Create("JOSE DA SILVA").Value!,
            TypeProduct.Water,
            Price.Create(100).Value!,
            StockQuantity.Create(20).Value!
        );

        var dto = product.Adapt<ProductResponse>();

        dto.Name.Should().Be("JOSE DA SILVA");
        dto.Price.Should().Be(100);
        dto.Quantity.Should().Be(20);
    }

    [Fact]
    public void Should_Not_Lose_Enum_Value_When_Mapping()
    {
        var product = new Product(
            ProductName.Create("Test").Value!,
            TypeProduct.Gas,
            Price.Create(50).Value!,
            StockQuantity.Create(10).Value!
        );

        var dto = product.Adapt<ProductResponse>();

        dto.Type.Should().Be(TypeProduct.Gas);
    }

    [Fact]
    public void Should_Have_Mapster_Configuration_Loaded()
    {
        var config = TypeAdapterConfig.GlobalSettings;

        config.Should().NotBeNull();
    }
}