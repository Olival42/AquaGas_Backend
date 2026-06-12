using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.Services;

public class RegisterSaleValidationFactoryTests
{
    [Fact]
    public void Should_Succeed_When_Input_Is_Valid()
    {
        var input = new RegisterSaleInput
        {
            CustomerId = Guid.NewGuid(),
            Discount = 10,
            SaleItems = new List<SaleItemsInput>
            {
                new SaleItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2
                }
            }
        };

        var result = RegisterSaleValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.SaleItems);
    }

    [Fact]
    public void Should_Fail_When_Discount_Is_Invalid()
    {
        var input = new RegisterSaleInput
        {
            CustomerId = Guid.NewGuid(),
            Discount = -10,
            SaleItems = new List<SaleItemsInput>
            {
                new SaleItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2
                }
            }
        };

        var result = RegisterSaleValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "VALIDATION_ERROR");
    }

    [Fact]
    public void Should_Fail_When_Quantity_Is_Invalid()
    {
        var input = new RegisterSaleInput
        {
            CustomerId = Guid.NewGuid(),
            Discount = 10,
            SaleItems = new List<SaleItemsInput>
            {
                new SaleItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = -1
                }
            }
        };

        var result = RegisterSaleValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("quantity"));
    }

    [Fact]
    public void Should_Fail_When_Multiple_Errors_Occur()
    {
        var input = new RegisterSaleInput
        {
            CustomerId = Guid.NewGuid(),
            Discount = 200,
            SaleItems = new List<SaleItemsInput>
            {
                new SaleItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = -1
                }
            }
        };

        var result = RegisterSaleValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.True(result.Errors.Count >= 2);
    }

    [Fact]
    public void Should_Ignore_Discount_When_Null()
    {
        var input = new RegisterSaleInput
        {
            CustomerId = Guid.NewGuid(),
            Discount = null,
            SaleItems = new List<SaleItemsInput>
            {
                new SaleItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            }
        };

        var result = RegisterSaleValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Discount);
    }

    [Fact]
    public void Should_Ignore_Invalid_Items_And_Return_Failure()
    {
        var input = new RegisterSaleInput
        {
            CustomerId = Guid.NewGuid(),
            Discount = 10,
            SaleItems = new List<SaleItemsInput>
        {
            new SaleItemsInput
            {
                ProductId = Guid.NewGuid(),
                Quantity = -1
            }
        }
        };

        var result = RegisterSaleValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
    }
}