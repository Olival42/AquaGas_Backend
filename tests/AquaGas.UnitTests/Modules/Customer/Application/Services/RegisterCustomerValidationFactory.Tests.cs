using Xunit;
using FluentAssertions;
using AquaGas.API.Modules.Customer.Application.Services;
using AquaGas.Api.Modules.Customer.Application.Dtos.Requests;

public class RegisterCustomerValidationFactoryTests
{
    private RegisterCustomerInput CreateValidInput()
    {
        return new RegisterCustomerInput
        {
            Name = "João",
            Document = "55964416004",
            Email = "email@email.com",
            Phone = "44999999999",
            Address = new RegisterAddressInput
            {
                Street = "Rua A",
                Number = "123",
                Neighborhood = "Centro",
                City = "Maringá",
                Cep = "87000000"
            }
        };
    }

    [Fact]
    public void Should_Return_Error_When_Input_Is_Null()
    {
        var result = RegisterCustomerValidationFactory.Combine(null!);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == "Input is required");
    }

    [Fact]
    public void Should_Return_Error_When_Address_Is_Null()
    {
        var input = CreateValidInput();
        input = input with { Address = null! };

        var result = RegisterCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == "Address is required");
    }

    [Fact]
    public void Should_Return_Error_When_Document_Is_Invalid()
    {
        var input = CreateValidInput();
        input = input with { Document = "123" };

        var result = RegisterCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_Email_Is_Invalid()
    {
        var input = CreateValidInput();
        input = input with { Email = "invalid-email" };

        var result = RegisterCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_Phone_Is_Invalid()
    {
        var input = CreateValidInput();
        input = input with { Phone = "123" };

        var result = RegisterCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_Cep_Is_Invalid()
    {
        var input = CreateValidInput();
        input = input with
        {
            Address = input.Address with { Cep = "123" }
        };

        var result = RegisterCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Multiple_Errors_When_Multiple_Invalid_Fields()
    {
        var input = new RegisterCustomerInput
        {
            Name = "",
            Document = "123",
            Email = "invalid",
            Phone = "1",
            Address = new RegisterAddressInput
            {
                Street = "",
                Number = "",
                Neighborhood = "",
                City = "",
                Cep = "1"
            }
        };

        var result = RegisterCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Should_Return_Validated_Object_When_Input_Is_Valid()
    {
        var input = CreateValidInput();

        var result = RegisterCustomerValidationFactory.Combine(input);

        result.IsSuccess.Should().BeTrue();

        var value = result.Value!;

        value.Name.Value.Should().Be(input.Name);
        value.Document.Value.Should().Be(input.Document);
        value.Email.Value.Should().Be(input.Email);
        value.Phone.Value.Should().Be(input.Phone);
        value.Address.Cep.Value.Should().Be(input.Address.Cep);
    }

    [Fact]
    public void Should_Normalize_Document()
    {
        var input = CreateValidInput();
        input = input with { Document = "559.644.160-04" };

        var result = RegisterCustomerValidationFactory.Combine(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Document.Value.Should().Be("55964416004");
    }
}