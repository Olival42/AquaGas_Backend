using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.Validators;
using FluentValidation.TestHelper;

public class RegisterSaleValidatorTests
{
    private readonly RegisterSaleValidator _validator = new();

    [Fact]
    public void Should_Fail_When_SaleItems_Is_Null()
    {
        var model = new RegisterSaleInput
        {
            SaleItems = null!,
            CustomerId = null
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.SaleItems)
              .WithErrorMessage("Sale items are required");
    }

    [Fact]
    public void Should_Fail_When_SaleItems_Is_Empty()
    {
        var model = new RegisterSaleInput
        {
            SaleItems = new List<SaleItemsInput>(),
            CustomerId = null
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.SaleItems)
              .WithErrorMessage("Sale must contain at least one item");
    }

    [Fact]
    public void Should_Fail_When_CustomerId_Is_Empty_Guid()
    {
        var model = new RegisterSaleInput
        {
            SaleItems = new List<SaleItemsInput>
            {
                new SaleItemsInput()
            },
            CustomerId = Guid.Empty
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.CustomerId)
              .WithErrorMessage("Customer id must be a valid UUID when informed");
    }

    [Fact]
    public void Should_Pass_When_CustomerId_Is_Null()
    {
        var model = new RegisterSaleInput
        {
            SaleItems = new List<SaleItemsInput>
            {
                new SaleItemsInput()
            },
            CustomerId = null
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void Should_Pass_When_Model_Is_Valid()
    {
        var model = new RegisterSaleInput
        {
            CustomerId = Guid.NewGuid(),
            SaleItems = new List<SaleItemsInput>
        {
            new SaleItemsInput
            {
                ProductId = Guid.NewGuid(),
                Quantity = 1
            }
        }
        };

        var validator = new RegisterSaleValidator();

        var result = validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}