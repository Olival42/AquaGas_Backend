using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Validators;
using FluentAssertions;
using Xunit;

namespace AquaGas.Plan.Application.Tests.Validators;

public sealed class RescheduleDeliveryInputValidatorTests
{
    private readonly RescheduleDeliveryInputValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Input_Is_Valid()
    {
        var input = new RescheduleDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            NewDate = DateTime.UtcNow.AddDays(2),
            Reason = "Cliente solicitou reagendamento"
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_DeliveryId_Is_Empty()
    {
        var input = new RescheduleDeliveryInput
        {
            DeliveryId = Guid.Empty,
            NewDate = DateTime.UtcNow.AddDays(2),
            Reason = "Cliente solicitou reagendamento"
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "DeliveryId is required");
    }

    [Fact]
    public void Should_Fail_When_NewDate_Is_Default()
    {
        var input = new RescheduleDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            NewDate = default,
            Reason = "Cliente solicitou reagendamento"
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "NewDate is required");
    }

    [Fact]
    public void Should_Fail_When_NewDate_Is_In_The_Past()
    {
        var input = new RescheduleDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            NewDate = DateTime.UtcNow.AddDays(-1),
            Reason = "Cliente solicitou reagendamento"
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "NewDate must be in the future");
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Empty()
    {
        var input = new RescheduleDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            NewDate = DateTime.UtcNow.AddDays(1),
            Reason = string.Empty
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "Reason is required");
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Null()
    {
        var input = new RescheduleDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            NewDate = DateTime.UtcNow.AddDays(1),
            Reason = null!
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "Reason is required");
    }

    [Fact]
    public void Should_Fail_When_Reason_Has_Less_Than_5_Characters()
    {
        var input = new RescheduleDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            NewDate = DateTime.UtcNow.AddDays(1),
            Reason = "1234"
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Reason must have at least 5 characters");
    }

    [Fact]
    public void Should_Fail_When_Reason_Exceeds_500_Characters()
    {
        var input = new RescheduleDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            NewDate = DateTime.UtcNow.AddDays(1),
            Reason = new string('a', 501)
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Reason must not exceed 500 characters");
    }

    [Fact]
    public void Should_Pass_When_Reason_Has_Exactly_5_Characters()
    {
        var input = new RescheduleDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            NewDate = DateTime.UtcNow.AddDays(1),
            Reason = "12345"
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_Reason_Has_Exactly_500_Characters()
    {
        var input = new RescheduleDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            NewDate = DateTime.UtcNow.AddDays(1),
            Reason = new string('a', 500)
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }
}