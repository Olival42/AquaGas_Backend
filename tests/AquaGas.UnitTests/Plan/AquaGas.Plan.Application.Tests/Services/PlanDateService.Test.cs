using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;

using FluentAssertions;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Services;

public sealed class PlanDateServiceTests
{
    private readonly PlanDateService _service = new();

    [Fact]
    public void Should_Adjust_Day_When_Day_Is_Valid()
    {
        var result = _service.AdjustDay(
            2025,
            5,
            15);

        result.Should().Be(
            new DateTime(
                2025,
                5,
                15,
                0,
                0,
                0,
                DateTimeKind.Utc));
    }

    [Fact]
    public void Should_Adjust_Day_To_Last_Day_Of_Month()
    {
        var result = _service.AdjustDay(
            2025,
            2,
            31);

        result.Should().Be(
            new DateTime(
                2025,
                2,
                28,
                0,
                0,
                0,
                DateTimeKind.Utc));
    }

    [Fact]
    public void Should_Adjust_Day_Correctly_In_Leap_Year()
    {
        var result = _service.AdjustDay(
            2024,
            2,
            31);

        result.Should().Be(
            new DateTime(
                2024,
                2,
                29,
                0,
                0,
                0,
                DateTimeKind.Utc));
    }

    [Fact]
    public void Should_Generate_EndDate_For_Monthly_Cycle()
    {
        var startDate = new DateTime(
            2025,
            1,
            10,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var result = _service.GenerateEndDate(
            startDate,
            PlanCycle.Monthly,
            10,
            15,
            null);

        result.Should().Be(
            new DateTime(
                2025,
                1,
                15,
                0,
                0,
                0,
                DateTimeKind.Utc));
    }

    [Fact]
    public void Should_Generate_EndDate_For_Quarterly_Cycle()
    {
        var startDate = new DateTime(
            2025,
            1,
            10,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var result = _service.GenerateEndDate(
            startDate,
            PlanCycle.Quarterly,
            5,
            20,
            null);

        result.Should().Be(
            new DateTime(
                2025,
                3,
                20,
                0,
                0,
                0,
                DateTimeKind.Utc));
    }

    [Fact]
    public void Should_Generate_EndDate_For_Annual_Cycle()
    {
        var startDate = new DateTime(
            2025,
            1,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var result = _service.GenerateEndDate(
            startDate,
            PlanCycle.Annual,
            10,
            25,
            null);

        result.Should().Be(
            new DateTime(
                2025,
                12,
                25,
                0,
                0,
                0,
                DateTimeKind.Utc));
    }

    [Fact]
    public void Should_Generate_EndDate_For_Custom_Cycle()
    {
        var startDate = new DateTime(
            2025,
            1,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var result = _service.GenerateEndDate(
            startDate,
            PlanCycle.Custom,
            10,
            20,
            6);

        result.Should().Be(
            new DateTime(
                2025,
                6,
                20,
                0,
                0,
                0,
                DateTimeKind.Utc));
    }

    [Fact]
    public void Should_Use_Max_Day_Between_Delivery_And_Billing()
    {
        var startDate = new DateTime(
            2025,
            1,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var result = _service.GenerateEndDate(
            startDate,
            PlanCycle.Monthly,
            5,
            28,
            null);

        result.Day.Should().Be(28);
    }

    [Fact]
    public void Should_Generate_StartDate_With_Valid_Lead_Time()
    {
        var result = _service.GenerateStartDate(
            10,
            15);

        result.Should().BeAfter(
            DateTime.UtcNow.AddDays(2));
    }

    [Fact]
    public void Should_Generate_StartDate_Using_Minimum_Reference_Day()
    {
        var result = _service.GenerateStartDate(
            5,
            20);

        result.Day.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public void Should_Generate_StartDate_In_Next_Month_When_Current_Month_Is_Invalid()
    {
        var nearEndOfMonth = DateTime.UtcNow.Date;

        var result = _service.GenerateStartDate(
            1,
            2);

        result.Date.Should().BeAfter(
            nearEndOfMonth.AddDays(2));
    }
}