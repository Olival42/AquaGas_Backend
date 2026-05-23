using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;

using FluentAssertions;

using Moq;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Services;

public sealed class PlanDeliveryServiceTests
{
    private readonly Mock<IPlanDateService> _dateServiceMock = new();

    private readonly PlanDeliveryService _service;

    public PlanDeliveryServiceTests()
    {
        _service = new PlanDeliveryService(
            _dateServiceMock.Object);
    }

    [Fact]
    public void Should_Generate_Deliveries_For_Monthly_Plan()
    {
        var planId = Guid.NewGuid();

        var startDate = new DateTime(2025, 1, 1);

        var endDate = new DateTime(2025, 3, 31);

        _dateServiceMock
            .Setup(x =>
                x.AdjustDay(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    10))
            .Returns<int, int, int>((year, month, _) =>
                new DateTime(year, month, 10));

        var result = _service.GenerateDeliveries(
            planId,
            startDate,
            endDate,
            PlanCycle.Monthly,
            10,
            null);

        result.Should().HaveCount(3);

        result[0].PlanId.Should().Be(planId);
        result[0].Period.Should().Be(1);
        result[0].DueDate.Should().Be(new DateTime(2025, 1, 10));

        result[1].Period.Should().Be(2);
        result[1].DueDate.Should().Be(new DateTime(2025, 2, 10));

        result[2].Period.Should().Be(3);
        result[2].DueDate.Should().Be(new DateTime(2025, 3, 10));
    }

    [Fact]
    public void Should_Generate_Single_Delivery_When_Start_And_End_Are_Same_Month()
    {
        var planId = Guid.NewGuid();

        var startDate = new DateTime(2025, 5, 1);

        var endDate = new DateTime(2025, 5, 31);

        _dateServiceMock
            .Setup(x =>
                x.AdjustDay(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    15))
            .Returns<int, int, int>((year, month, _) =>
                new DateTime(year, month, 15));

        var result = _service.GenerateDeliveries(
            planId,
            startDate,
            endDate,
            PlanCycle.Monthly,
            15,
            null);

        result.Should().HaveCount(1);

        result[0].Period.Should().Be(1);

        result[0].DueDate.Should().Be(
            new DateTime(2025, 5, 15));
    }

    [Fact]
    public void Should_Call_AdjustDay_For_Each_Month()
    {
        var planId = Guid.NewGuid();

        var startDate = new DateTime(2025, 1, 1);

        var endDate = new DateTime(2025, 4, 30);

        _dateServiceMock
            .Setup(x =>
                x.AdjustDay(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
            .Returns<int, int, int>((year, month, day) =>
                new DateTime(year, month, day));

        _service.GenerateDeliveries(
            planId,
            startDate,
            endDate,
            PlanCycle.Monthly,
            20,
            null);

        _dateServiceMock.Verify(
            x => x.AdjustDay(
                It.IsAny<int>(),
                It.IsAny<int>(),
                20),
            Times.Exactly(4));
    }

    [Fact]
    public void Should_Generate_Sequential_Periods()
    {
        var planId = Guid.NewGuid();

        var startDate = new DateTime(2025, 1, 1);

        var endDate = new DateTime(2025, 6, 30);

        _dateServiceMock
            .Setup(x =>
                x.AdjustDay(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
            .Returns<int, int, int>((year, month, day) =>
                new DateTime(year, month, day));

        var result = _service.GenerateDeliveries(
            planId,
            startDate,
            endDate,
            PlanCycle.Monthly,
            5,
            null);

        result.Should().HaveCount(6);

        for (int i = 0; i < result.Count; i++)
        {
            result[i].Period.Should().Be(i + 1);
        }
    }

    [Fact]
    public void Should_Return_Empty_List_When_StartDate_Is_Greater_Than_EndDate()
    {
        var result = _service.GenerateDeliveries(
            Guid.NewGuid(),
            new DateTime(2025, 5, 1),
            new DateTime(2025, 4, 1),
            PlanCycle.Monthly,
            10,
            null);

        result.Should().BeEmpty();
    }
}