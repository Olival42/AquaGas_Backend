using AquaGas.Plan.Application.Services;

using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Results;

using FluentAssertions;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Services;

public sealed class PlanCalculationServiceTests
{
    private readonly PlanCalculationService _service = new();

    [Fact]
    public void Should_Calculate_Contract_Total_Without_Discount()
    {
        var result = _service.CalculateContractTotal(
            monthlySubtotal: 100,
            numberOfMonths: 6,
            discount: null);

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().NotBeNull();

        result.Value!.Value.Should().Be(600);
    }

    [Fact]
    public void Should_Calculate_Contract_Total_With_Discount()
    {
        var discount = Discount.Create(10)
            .Value!;

        var result = _service.CalculateContractTotal(
            monthlySubtotal: 200,
            numberOfMonths: 5,
            discount: discount);

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().NotBeNull();

        result.Value!.Value.Should().Be(900);
    }

    [Fact]
    public void Should_Return_Failure_When_Contract_Total_Is_Invalid()
    {
        var result = _service.CalculateContractTotal(
            monthlySubtotal: -100,
            numberOfMonths: 2,
            discount: null);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Should_Calculate_Monthly_Billing_Successfully()
    {
        var contractTotal = Price.Create(1200)
            .Value!;

        var result = _service.CalculateMonthlyBilling(
            contractTotal,
            numberOfBillings: 12);

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().NotBeNull();

        result.Value!.Value.Should().Be(100);
    }

    [Fact]
    public void Should_Return_Failure_When_Number_Of_Billings_Is_Zero()
    {
        var contractTotal = Price.Create(1000)
            .Value!;

        var result = _service.CalculateMonthlyBilling(
            contractTotal,
            numberOfBillings: 0);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should()
            .Contain(x =>
                x.Message ==
                "Number of billings must be greater than zero");
    }

    [Fact]
    public void Should_Return_Failure_When_Number_Of_Billings_Is_Negative()
    {
        var contractTotal = Price.Create(1000)
            .Value!;

        var result = _service.CalculateMonthlyBilling(
            contractTotal,
            numberOfBillings: -5);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should()
            .Contain(x =>
                x.Message ==
                "Number of billings must be greater than zero");
    }

    [Fact]
    public void Should_Return_Failure_When_Monthly_Billing_Result_Is_Invalid()
    {
        var contractTotal = Price.Create(-100)
            .Value;

        if (contractTotal is null)
            return;

        var result = _service.CalculateMonthlyBilling(
            contractTotal,
            numberOfBillings: 2);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Calculate_Monthly_Billing_With_Decimal_Result()
    {
        var contractTotal = Price.Create(1000)
            .Value!;

        var result = _service.CalculateMonthlyBilling(
            contractTotal,
            numberOfBillings: 3);

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().NotBeNull();

        result.Value!.Value.Should().BeApproximately(
            333.33m,
            0.01m);
    }
}