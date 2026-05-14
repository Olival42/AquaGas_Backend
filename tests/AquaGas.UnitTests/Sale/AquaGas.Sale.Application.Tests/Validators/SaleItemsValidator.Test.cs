using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.Validators;
using FluentValidation.TestHelper;

public class SaleItemsValidatorTests
{
    private readonly SaleItemsValidator _validator = new();

    [Fact]
    public void Should_Fail_When_ProductId_Is_Empty()
    {
        var model = new SaleItemsInput
        {
            ProductId = Guid.Empty,
            Quantity = 1
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.ProductId)
              .WithErrorMessage("Product id is required");
    }

    [Fact]
    public void Should_Fail_When_ProductId_Is_Invalid()
    {
        var model = new SaleItemsInput
        {
            ProductId = Guid.Empty,
            Quantity = 1
        };

        var result = _validator.TestValidate(model);

        // Como você usa CascadeMode.Stop, o primeiro erro já interrompe.
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void Should_Pass_When_ProductId_Is_Valid()
    {
        var model = new SaleItemsInput
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void Should_Fail_When_Quantity_Is_Zero()
    {
        var model = new SaleItemsInput
        {
            ProductId = Guid.NewGuid(),
            Quantity = 0
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Quantity)
              .WithErrorMessage("Quantity must be greater than zero");
    }

    [Fact]
    public void Should_Fail_When_Quantity_Is_Negative()
    {
        var model = new SaleItemsInput
        {
            ProductId = Guid.NewGuid(),
            Quantity = -5
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Quantity)
              .WithErrorMessage("Quantity must be greater than zero");
    }

    [Fact]
    public void Should_Pass_When_Quantity_Is_Greater_Than_Zero()
    {
        var model = new SaleItemsInput
        {
            ProductId = Guid.NewGuid(),
            Quantity = 10
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Model_Is_Valid()
    {
        var model = new SaleItemsInput
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}