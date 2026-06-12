using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Factories;
using FluentAssertions;
using Xunit;

namespace AquaGas.Plan.Domain.Tests.Factories;

public class PlanCycleFactoryTests
{
    [Fact]
    public void Should_Create_Monthly_Cycle()
    {
        // Arrange & Act
        var result = PlanCycleFactory.Create("Monthly");

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().Be(PlanCycle.Monthly);
    }

    [Fact]
    public void Should_Create_Quarterly_Cycle()
    {
        // Arrange & Act
        var result = PlanCycleFactory.Create("Quarterly");

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().Be(PlanCycle.Quarterly);
    }

    [Fact]
    public void Should_Create_Annual_Cycle()
    {
        // Arrange & Act
        var result = PlanCycleFactory.Create("Annual");

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().Be(PlanCycle.Annual);
    }

    [Fact]
    public void Should_Create_Custom_Cycle()
    {
        // Arrange & Act
        var result = PlanCycleFactory.Create("Custom");

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().Be(PlanCycle.Custom);
    }

    [Fact]
    public void Should_Trim_Input_Before_Creating()
    {
        // Arrange & Act
        var result = PlanCycleFactory.Create("  Monthly  ");

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().Be(PlanCycle.Monthly);
    }

    [Fact]
    public void Should_Return_Error_When_Input_Is_Null()
    {
        // Arrange & Act
        var result = PlanCycleFactory.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Errors.FirstOrDefault()!.Code.Should().Be("VALIDATION_ERROR");

        result.Errors.FirstOrDefault()!.Message.Should()
            .Be("PlanCycle is required");
    }

    [Fact]
    public void Should_Return_Error_When_Input_Is_Empty()
    {
        // Arrange & Act
        var result = PlanCycleFactory.Create("");

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Errors.FirstOrDefault()!.Message.Should()
            .Be("PlanCycle is required");
    }

    [Fact]
    public void Should_Return_Error_When_Input_Is_Whitespace()
    {
        // Arrange & Act
        var result = PlanCycleFactory.Create("   ");

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Errors.FirstOrDefault()!.Message.Should()
            .Be("PlanCycle is required");
    }

    [Fact]
    public void Should_Return_Error_When_Cycle_Is_Invalid()
    {
        // Arrange & Act
        var result = PlanCycleFactory.Create("Weekly");

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Errors.FirstOrDefault()!.Message.Should()
            .Be("Plan cycle is invalid. Allowed: Monthly, Quarterly, Annual, Custom");
    }

    [Theory]
    [InlineData("monthly")]
    [InlineData("MONTHLY")]
    [InlineData("annual")]
    [InlineData("quarter")]
    [InlineData("test")]
    public void Should_Return_Error_For_Invalid_Case_Or_Value(string input)
    {
        // Arrange & Act
        var result = PlanCycleFactory.Create(input);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}