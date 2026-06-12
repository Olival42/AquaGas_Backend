using Xunit;
using FluentAssertions;
using AquaGas.Customer.Application.Services;
using AquaGas.Customer.Application.Dtos.Requests;

public class UpdateCustomerValidationFactoryTests
{
    private UpdateCustomerInput CreateEmptyInput()
    {
        return new UpdateCustomerInput();
    }

    [Fact]
    public void Should_Succeed_When_No_Fields_Provided()
    {
        var input = CreateEmptyInput();

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void Should_Return_Error_When_Name_Is_Invalid()
    {
        var input = new UpdateCustomerInput
        {
            Name = ""
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_Document_Is_Invalid()
    {
        var input = new UpdateCustomerInput
        {
            Document = "123"
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_Email_Is_Invalid()
    {
        var input = new UpdateCustomerInput
        {
            Email = "invalid"
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_Phone_Is_Invalid()
    {
        var input = new UpdateCustomerInput
        {
            Phone = "123"
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_AddressId_Is_Missing()
    {
        var input = new UpdateCustomerInput
        {
            Address = new UpdateAddressInput
            {
                Street = "Rua A"
            }
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("AddressId"));
    }

    [Fact]
    public void Should_Return_Error_When_AddressId_Is_Empty()
    {
        var input = new UpdateCustomerInput
        {
            Address = new UpdateAddressInput
            {
                AddressId = Guid.Empty
            }
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_Street_Is_Too_Short()
    {
        var input = new UpdateCustomerInput
        {
            Address = new UpdateAddressInput
            {
                AddressId = Guid.NewGuid(),
                Street = "A"
            }
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_Number_Is_Empty()
    {
        var input = new UpdateCustomerInput
        {
            Address = new UpdateAddressInput
            {
                AddressId = Guid.NewGuid(),
                Number = "   "
            }
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Number cannot be empty"));
    }

    [Fact]
    public void Should_Return_Error_When_Number_Is_Too_Long()
    {
        var input = new UpdateCustomerInput
        {
            Address = new UpdateAddressInput
            {
                AddressId = Guid.NewGuid(),
                Number = "1234567891011"
            }
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_Complement_Is_Too_Long()
    {
        var input = new UpdateCustomerInput
        {
            Address = new UpdateAddressInput
            {
                AddressId = Guid.NewGuid(),
                Complement = new string('A', 101)
            }
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Error_When_Cep_Is_Invalid()
    {
        var input = new UpdateCustomerInput
        {
            Address = new UpdateAddressInput
            {
                AddressId = Guid.NewGuid(),
                Cep = "123"
            }
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Multiple_Errors()
    {
        var input = new UpdateCustomerInput
        {
            Name = "",
            Email = "invalid",
            Address = new UpdateAddressInput
            {
                AddressId = Guid.Empty,
                Street = "A"
            }
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void Should_Return_Validated_Object_When_Address_Is_Valid()
    {
        var input = new UpdateCustomerInput
        {
            Name = "João",
            Address = new UpdateAddressInput
            {
                AddressId = Guid.NewGuid(),
                Street = "Rua A",
                Neighborhood = "Centro",
                City = "Maringá",
                Number = "123",
                Cep = "87000000"
            }
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsSuccess.Should().BeTrue();

        var address = result.Value!.Address!;

        address.Street.Should().Be("Rua A");
        address.Number.Should().Be("123");
    }

    [Fact]
    public void Should_Normalize_Complement()
    {
        var input = new UpdateCustomerInput
        {
            Address = new UpdateAddressInput
            {
                AddressId = Guid.NewGuid(),
                Complement = "  Apto   101   "
            }
        };

        var result = UpdateCustomerValidationFactory.Combine(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Address!.Complement.Should().Be("Apto 101");
    }
}