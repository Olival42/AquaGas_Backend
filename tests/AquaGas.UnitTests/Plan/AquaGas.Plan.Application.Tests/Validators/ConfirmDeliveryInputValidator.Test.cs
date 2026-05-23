using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Validators;

using FluentAssertions;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Validators;

public sealed class ConfirmDeliveryInputValidatorTests
{
    private readonly ConfirmDeliveryInputValidator _validator = new();

    private static ConfirmDeliveryInput CreateValidInput()
    {
        return new ConfirmDeliveryInput
        {
            DeliveryId = Guid.NewGuid()
        };
    }

    [Fact]
    public void Should_Pass_When_Input_Is_Valid()
    {
        var input = CreateValidInput();

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_DeliveryId_Is_Empty()
    {
        var input = CreateValidInput() with
        {
            DeliveryId = Guid.Empty
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "DeliveryId is required");
    }
}