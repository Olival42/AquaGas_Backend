using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Shared.Domain.ValueObjects;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

using FluentAssertions;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Services;

public sealed class ContractPenaltyServiceTests
{
    private readonly ContractPenaltyService _service = new();

    private static PlanEntity CreatePlan(decimal total)
    {
        return new PlanEntity(
            customerId: Guid.NewGuid(),
            employeeId: Guid.NewGuid(),
            cycle: PlanCycle.Monthly,
            total: Price.Create(total).Value!,
            startDate: DateTime.UtcNow,
            endDate: DateTime.UtcNow.AddMonths(1),
            deliveryDay: 10,
            billingDay: 15,
            currentDiscount: null);
    }

    private static Billing CreateBilling(
    BillingStatus status,
    decimal amount)
    {
        var billing = new Billing(
            Guid.NewGuid(),
            1,
            DateTime.UtcNow,
            Price.Create(amount).Value!);

        typeof(Billing)
            .GetProperty(nameof(Billing.Status))!
            .SetValue(billing, status);

        return billing;
    }

    [Fact]
    public void Should_Return_Null_When_No_Remaining_Billings()
    {
        var plan = CreatePlan(1000m);

        var billings = new List<Billing>
        {
            CreateBilling(BillingStatus.Paid, 500m),
            CreateBilling(BillingStatus.Canceled, 500m)
        };

        var result = _service.CalculateCancellationPenalty(
            plan,
            billings,
            Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public void Should_Return_Null_When_Sum_Is_Zero()
    {
        var plan = CreatePlan(1000m);

        var billings = new List<Billing>
        {
            CreateBilling(BillingStatus.Paid, 0m)
        };

        var result = _service.CalculateCancellationPenalty(
            plan,
            billings,
            Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public void Should_Calculate_Penalty_When_Pending_Billings_Exist()
    {
        var plan = CreatePlan(1000m);

        var billings = new List<Billing>
        {
            CreateBilling(BillingStatus.Pending, 300m),
            CreateBilling(BillingStatus.Late, 200m),
            CreateBilling(BillingStatus.Paid, 500m)
        };

        var userId = Guid.NewGuid();

        var result = _service.CalculateCancellationPenalty(
            plan,
            billings,
            userId,
            "cancelamento");

        result.Should().NotBeNull();
        result!.PlanId.Should().Be(plan.Id);
        result.Type.Should().Be(ContractPenaltyType.EarlyCancellation);

        result.RemainingValue.Value.Should().Be(500m);
        result.CalculatedAmount.Value.Should().Be(500m);
        result.OriginalValue.Value.Should().Be(1000m);
    }

    [Fact]
    public void Should_Ignore_Paid_And_Canceled_Billings()
    {
        var plan = CreatePlan(1000m);

        var billings = new List<Billing>
        {
            CreateBilling(BillingStatus.Paid, 300m),
            CreateBilling(BillingStatus.Canceled, 200m),
            CreateBilling(BillingStatus.Pending, 100m)
        };

        var result = _service.CalculateCancellationPenalty(
            plan,
            billings,
            Guid.NewGuid());

        result.Should().NotBeNull();
        result!.RemainingValue.Value.Should().Be(100m);
    }

    [Fact]
    public void Should_Use_Notes_When_Provided()
    {
        var plan = CreatePlan(500m);

        var billings = new List<Billing>
        {
            CreateBilling(BillingStatus.Pending, 100m)
        };

        var result = _service.CalculateCancellationPenalty(
            plan,
            billings,
            Guid.NewGuid(),
            "cliente solicitou cancelamento");

        result.Should().NotBeNull();
        result!.Notes.Should().Be("cliente solicitou cancelamento");
    }
}