using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;

using FluentAssertions;

using Moq;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Services;

public sealed class PlanBillingServiceTests
{
    private readonly Mock<IPlanDateService> _dateServiceMock;
    private readonly PlanBillingService _service;

    public PlanBillingServiceTests()
    {
        _dateServiceMock = new Mock<IPlanDateService>();

        _service = new PlanBillingService(
            _dateServiceMock.Object);
    }

    [Fact]
    public void Should_Generate_Monthly_Billings()
    {
        var planId = Guid.NewGuid();

        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 3, 31);

        var totalAmount =
            Price.Create(100m).Value!;

        _dateServiceMock
            .Setup(x =>
                x.AdjustDay(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    10))
            .Returns<int, int, int>((year, month, _) =>
                new DateTime(year, month, 10));

        var result = _service.GenerateBillings(
            planId,
            startDate,
            endDate,
            PlanCycle.Monthly,
            10,
            totalAmount,
            null);

        result.Should().HaveCount(3);

        result[0].Period.Should().Be(1);
        result[1].Period.Should().Be(2);
        result[2].Period.Should().Be(3);

        result[0].DueDate.Should()
            .Be(new DateTime(2025, 1, 10));

        result[1].DueDate.Should()
            .Be(new DateTime(2025, 2, 10));

        result[2].DueDate.Should()
            .Be(new DateTime(2025, 3, 10));
    }

    [Fact]
    public void Should_Create_Billings_With_Correct_Amount()
    {
        var planId = Guid.NewGuid();

        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);

        var totalAmount =
            Price.Create(250m).Value!;

        _dateServiceMock
            .Setup(x =>
                x.AdjustDay(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
            .Returns<int, int, int>((year, month, day) =>
                new DateTime(year, month, day));

        var result = _service.GenerateBillings(
            planId,
            startDate,
            endDate,
            PlanCycle.Monthly,
            15,
            totalAmount,
            null);

        result.Should().HaveCount(1);

        result[0].Amount.Value.Should().Be(250m);
    }

    [Fact]
    public void Should_Call_AdjustDay_For_Each_Billing()
    {
        var planId = Guid.NewGuid();

        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 4, 30);

        var totalAmount =
            Price.Create(100m).Value!;

        _dateServiceMock
            .Setup(x =>
                x.AdjustDay(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
            .Returns<int, int, int>((year, month, day) =>
                new DateTime(year, month, day));

        _service.GenerateBillings(
            planId,
            startDate,
            endDate,
            PlanCycle.Monthly,
            20,
            totalAmount,
            null);

        _dateServiceMock.Verify(
            x => x.AdjustDay(
                It.IsAny<int>(),
                It.IsAny<int>(),
                20),
            Times.Exactly(4));
    }

    [Fact]
    public void Should_Return_Empty_List_When_StartDate_Is_After_EndDate()
    {
        var planId = Guid.NewGuid();

        var startDate = new DateTime(2025, 5, 1);
        var endDate = new DateTime(2025, 4, 1);

        var totalAmount =
            Price.Create(100m).Value!;

        var result = _service.GenerateBillings(
            planId,
            startDate,
            endDate,
            PlanCycle.Monthly,
            10,
            totalAmount,
            null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Should_Generate_Sequential_Periods()
    {
        var planId = Guid.NewGuid();

        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 5, 31);

        var totalAmount =
            Price.Create(100m).Value!;

        _dateServiceMock
            .Setup(x =>
                x.AdjustDay(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
            .Returns<int, int, int>((year, month, day) =>
                new DateTime(year, month, day));

        var result = _service.GenerateBillings(
            planId,
            startDate,
            endDate,
            PlanCycle.Monthly,
            5,
            totalAmount,
            null);

        result.Select(x => x.Period)
            .Should()
            .ContainInOrder(1, 2, 3, 4, 5);
    }
}