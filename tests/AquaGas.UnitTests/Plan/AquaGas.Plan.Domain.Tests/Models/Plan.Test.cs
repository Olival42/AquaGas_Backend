using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using AquaGas.Shared.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AquaGas.Plan.Domain.Tests.Models;

public class PlanTests
{
    private static PlanEntity CreatePlan()
    {
        return new PlanEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(200).Value!,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1),
            10,
            5
        );
    }

    [Fact]
    public void Should_Create_Plan_Correctly()
    {
        // Arrange & Act
        var plan = CreatePlan();

        // Assert
        plan.Id.Should().NotBeEmpty();

        plan.Status.Should().Be(PlanStatus.Active);

        plan.IsActive.Should().BeTrue();

        plan.Cycle.Should().Be(PlanCycle.Monthly);

        plan.Total.Value.Should().Be(200);

        plan.Items.Should().BeEmpty();
    }

    [Fact]
    public void Should_Add_Item_Correctly()
    {
        // Arrange
        var plan = CreatePlan();

        var item = new PlanItem(
            plan.Id,
            Guid.NewGuid(),
            StockQuantity.Create(2).Value!
        );

        // Act
        plan.AddItem(item);

        // Assert
        plan.Items.Should().ContainSingle();

        plan.Items.First().Should().Be(item);
    }

    [Fact]
    public void Should_Remove_Item_Correctly()
    {
        // Arrange
        var plan = CreatePlan();

        var item = new PlanItem(
            plan.Id,
            Guid.NewGuid(),
            StockQuantity.Create(2).Value!
        );

        plan.AddItem(item);

        // Act
        plan.RemoveItem(item);

        // Assert
        plan.Items.Should().BeEmpty();
    }

    [Fact]
    public void Should_Clear_Items_Correctly()
    {
        // Arrange
        var plan = CreatePlan();

        plan.AddItem(new PlanItem(
            plan.Id,
            Guid.NewGuid(),
            StockQuantity.Create(1).Value!
        ));

        plan.AddItem(new PlanItem(
            plan.Id,
            Guid.NewGuid(),
            StockQuantity.Create(2).Value!
        ));

        // Act
        plan.ClearItems();

        // Assert
        plan.Items.Should().BeEmpty();
    }

    [Fact]
    public void Should_Cancel_Plan_Correctly()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        plan.Cancel();

        // Assert
        plan.Status.Should().Be(PlanStatus.Canceled);

        plan.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Should_Suspend_Plan_When_Active()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        plan.Suspend("Pagamento pendente");

        // Assert
        plan.Status.Should().Be(PlanStatus.Suspended);

        plan.IsActive.Should().BeFalse();

        plan.SuspensionReason.Should().Be("Pagamento pendente");
    }

    [Fact]
    public void Should_Not_Suspend_When_Not_Active()
    {
        // Arrange
        var plan = CreatePlan();

        plan.Cancel();

        // Act
        plan.Suspend("Teste");

        // Assert
        plan.Status.Should().Be(PlanStatus.Canceled);
    }

    [Fact]
    public void Should_Reactivate_Suspended_Plan()
    {
        // Arrange
        var plan = CreatePlan();

        plan.Suspend("Motivo");

        // Act
        plan.Reactivate();

        // Assert
        plan.Status.Should().Be(PlanStatus.Active);

        plan.IsActive.Should().BeTrue();

        plan.SuspensionReason.Should().BeNull();
    }

    [Fact]
    public void Should_Not_Reactivate_When_Not_Suspended()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        plan.Reactivate();

        // Assert
        plan.Status.Should().Be(PlanStatus.Active);
    }

    [Fact]
    public void Should_Update_Discount_Correctly()
    {
        // Arrange
        var plan = CreatePlan();

        var discount = Discount.Create(10).Value!;

        // Act
        plan.UpdateDiscount(discount);

        // Assert
        plan.CurrentDiscount.Should().Be(discount);
    }

    [Fact]
    public void Should_Update_Total_Correctly()
    {
        // Arrange
        var plan = CreatePlan();

        var newTotal = Price.Create(500).Value!;

        // Act
        plan.UpdateTotal(newTotal);

        // Assert
        plan.Total.Should().Be(newTotal);

        plan.Total.Value.Should().Be(500);
    }

    [Fact]
    public void Should_Set_Awaiting_Closure_When_Active()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        plan.SetAwaitingClosure();

        // Assert
        plan.Status.Should().Be(PlanStatus.AwaitingClosure);
    }

    [Fact]
    public void Should_Not_Set_Awaiting_Closure_When_Not_Active()
    {
        // Arrange
        var plan = CreatePlan();

        plan.Cancel();

        // Act
        plan.SetAwaitingClosure();

        // Assert
        plan.Status.Should().Be(PlanStatus.Canceled);
    }

    [Fact]
    public void Should_Finish_Active_Plan()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        plan.Finish();

        // Assert
        plan.Status.Should().Be(PlanStatus.Finished);

        plan.IsActive.Should().BeFalse();

        plan.FinishedAt.Should().NotBeNull();
    }

    [Fact]
    public void Should_Finish_Awaiting_Closure_Plan()
    {
        // Arrange
        var plan = CreatePlan();

        plan.SetAwaitingClosure();

        // Act
        plan.Finish();

        // Assert
        plan.Status.Should().Be(PlanStatus.Finished);
    }

    [Fact]
    public void Should_Not_Finish_Canceled_Plan()
    {
        // Arrange
        var plan = CreatePlan();

        plan.Cancel();

        // Act
        plan.Finish();

        // Assert
        plan.Status.Should().Be(PlanStatus.Canceled);
    }

    [Fact]
    public void Should_Upgrade_Plan_Correctly()
    {
        // Arrange
        var plan = CreatePlan();

        var newTotal = Price.Create(1200).Value!;

        var newEndDate = DateTime.UtcNow.AddYears(1);

        // Act
        plan.Upgrade(
            PlanCycle.Annual,
            newTotal,
            newEndDate
        );

        // Assert
        plan.Cycle.Should().Be(PlanCycle.Annual);

        plan.Total.Should().Be(newTotal);

        plan.EndDate.Should().Be(
            DateTime.SpecifyKind(newEndDate, DateTimeKind.Utc)
        );
    }

    [Fact]
    public void Should_Not_Upgrade_When_Not_Active()
    {
        // Arrange
        var plan = CreatePlan();

        plan.Cancel();

        var originalCycle = plan.Cycle;

        // Act
        plan.Upgrade(
            PlanCycle.Annual,
            Price.Create(1000).Value!,
            DateTime.UtcNow.AddYears(1)
        );

        // Assert
        plan.Cycle.Should().Be(originalCycle);
    }

    [Fact]
    public void Should_Downgrade_Plan_Correctly()
    {
        // Arrange
        var plan = CreatePlan();

        plan.Upgrade(
            PlanCycle.Annual,
            Price.Create(1200).Value!,
            DateTime.UtcNow.AddYears(1)
        );

        var newTotal = Price.Create(150).Value!;

        var newEndDate = DateTime.UtcNow.AddMonths(1);

        // Act
        plan.Downgrade(
            PlanCycle.Monthly,
            newTotal,
            newEndDate
        );

        // Assert
        plan.Cycle.Should().Be(PlanCycle.Monthly);

        plan.Total.Should().Be(newTotal);

        plan.EndDate.Should().Be(
            DateTime.SpecifyKind(newEndDate, DateTimeKind.Utc)
        );
    }

    [Fact]
    public void Should_Not_Downgrade_When_Not_Active()
    {
        // Arrange
        var plan = CreatePlan();

        plan.Cancel();

        var originalTotal = plan.Total;

        // Act
        plan.Downgrade(
            PlanCycle.Monthly,
            Price.Create(50).Value!,
            DateTime.UtcNow.AddMonths(1)
        );

        // Assert
        plan.Total.Should().Be(originalTotal);
    }
}