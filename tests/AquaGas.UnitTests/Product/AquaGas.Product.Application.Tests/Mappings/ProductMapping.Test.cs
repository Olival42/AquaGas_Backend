using AquaGas.Product.Application.Mappings;
using AquaGas.Product.Application.Dtos.Resposes;
using AquaGas.Product.Application.Dtos.Responses;
using AquaGas.Product.Domain.Enums;
using ProductEntity = AquaGas.Product.Domain.Models.Product;
using AquaGas.Product.Domain.ValueObjects;
using Mapster;
using Xunit;

namespace AquaGas.Tests.Modules.Product.Application.Mappings;

public class ProductMappingTests
{
    public ProductMappingTests()
    {
        ProductMapping.Register();
    }

    [Fact]
    public void Register_Should_Map_RegisterProductValidated_To_ProductEntity()
    {
        var validated = new RegisterProductValidated
        {
            Name = ProductName.Create("Água Crystal").Value!,
            Type = TypeProduct.Water,
            Price = Price.Create(12.50m).Value!,
            Quantity = StockQuantity.Create(20).Value!
        };

        var product = validated.Adapt<ProductEntity>();

        Assert.NotNull(product);

        Assert.Equal("Água Crystal", product.Name.Value);
        Assert.Equal(TypeProduct.Water, product.Type);
        Assert.Equal(12.50m, product.Price.Value);
        Assert.Equal(20, product.Quantity.Value);
    }

    [Fact]
    public void Register_Should_Map_ProductEntity_To_ProductResponse()
    {
        var product = new ProductEntity(
            ProductName.Create("Gás P13").Value!,
            TypeProduct.Gas,
            Price.Create(120.90m).Value!,
            StockQuantity.Create(15).Value!
        );

        var response = product.Adapt<ProductResponse>();

        Assert.NotNull(response);

        Assert.Equal(product.Id, response.Id);
        Assert.Equal("Gás P13", response.Name);
        Assert.Equal(TypeProduct.Gas, response.Type);
        Assert.Equal(120.90m, response.Price);
        Assert.Equal(15, response.Quantity);
    }

    [Fact]
    public void Register_Should_Map_ProductEntity_With_Zero_Quantity()
    {
        var product = new ProductEntity(
            ProductName.Create("Água Mineral").Value!,
            TypeProduct.Water,
            Price.Create(5.99m).Value!,
            StockQuantity.Create(0).Value!
        );

        var response = product.Adapt<ProductResponse>();

        Assert.Equal(0, response.Quantity);
    }

    [Fact]
    public void Register_Should_Map_ProductEntity_With_High_Price()
    {
        var product = new ProductEntity(
            ProductName.Create("Gás Industrial").Value!,
            TypeProduct.Gas,
            Price.Create(9999.99m).Value!,
            StockQuantity.Create(100).Value!
        );

        var response = product.Adapt<ProductResponse>();

        Assert.Equal(9999.99m, response.Price);
    }

    [Fact]
    public void Register_Should_Preserve_Accented_Characters()
    {
        var product = new ProductEntity(
            ProductName.Create("Água São Lourenço").Value!,
            TypeProduct.Water,
            Price.Create(7.75m).Value!,
            StockQuantity.Create(30).Value!
        );

        var response = product.Adapt<ProductResponse>();

        Assert.Equal("Água São Lourenço", response.Name);
    }

    [Fact]
    public void Register_Should_Map_Different_Product_Types_Correctly()
    {
        var water = new ProductEntity(
            ProductName.Create("Água").Value!,
            TypeProduct.Water,
            Price.Create(4).Value!,
            StockQuantity.Create(10).Value!
        );

        var gas = new ProductEntity(
            ProductName.Create("Gás").Value!,
            TypeProduct.Gas,
            Price.Create(100).Value!,
            StockQuantity.Create(5).Value!
        );

        var waterResponse = water.Adapt<ProductResponse>();
        var gasResponse = gas.Adapt<ProductResponse>();

        Assert.Equal(TypeProduct.Water, waterResponse.Type);
        Assert.Equal(TypeProduct.Gas, gasResponse.Type);
    }

    [Fact]
    public void Register_Should_Map_ProductEntity_Id_Correctly()
    {
        var product = new ProductEntity(
            ProductName.Create("Produto").Value!,
            TypeProduct.Water,
            Price.Create(10).Value!,
            StockQuantity.Create(1).Value!
        );

        var response = product.Adapt<ProductResponse>();

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(product.Id, response.Id);
    }

    [Fact]
    public void Register_Should_Map_Rounded_Price_Correctly()
    {
        var product = new ProductEntity(
            ProductName.Create("Produto").Value!,
            TypeProduct.Water,
            Price.Create(10.999m).Value!,
            StockQuantity.Create(1).Value!
        );

        var response = product.Adapt<ProductResponse>();

        Assert.Equal(11.00m, response.Price);
    }

    [Fact]
    public void Register_Should_Create_Independent_Response_Instances()
    {
        var product = new ProductEntity(
            ProductName.Create("Água").Value!,
            TypeProduct.Water,
            Price.Create(8).Value!,
            StockQuantity.Create(10).Value!
        );

        var response1 = product.Adapt<ProductResponse>();
        var response2 = product.Adapt<ProductResponse>();

        Assert.NotSame(response1, response2);

        Assert.Equal(response1.Name, response2.Name);
        Assert.Equal(response1.Price, response2.Price);
    }
}