using Xunit;
using FluentValidation.TestHelper;
using AquaGas.Api.Modules.Customer.Application.Validators;
using AquaGas.Api.Modules.Customer.Application.Dtos.Requests;

public class RegisterCustomerValidatorTests
{
    private readonly RegisterCustomerValidator _validator;

    public RegisterCustomerValidatorTests()
    {
        _validator = new RegisterCustomerValidator();
    }

    private RegisterCustomerInput CreateValidInput(
        string name = "João Silva",
        string document = "12345678900",
        string email = "joao@email.com",
        string phone = "44999999999",
        RegisterAddressInput? address = null
    )
    {
        return new RegisterCustomerInput
        {
            Name = name,
            Document = document,
            Email = email,
            Phone = phone,
            Address = address ?? CreateValidAddress()
        };
    }

    private RegisterAddressInput CreateValidAddress(
        string street = "Rua A",
        string number = "123",
        string neighborhood = "Centro",
        string city = "Maringá",
        string cep = "87000000"
    )
    {
        return new RegisterAddressInput
        {
            Street = street,
            Number = number,
            Neighborhood = neighborhood,
            City = city,
            Cep = cep
        };
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Input_Is_Valid()
    {
        var input = CreateValidInput();

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var input = CreateValidInput(name: "");

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required");
    }

    [Fact]
    public void Should_Have_Error_When_Document_Is_Empty()
    {
        var input = CreateValidInput(document: "");

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Document)
              .WithErrorMessage("Document is required");
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        var input = CreateValidInput(email: "");

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required");
    }

    [Fact]
    public void Should_Have_Error_When_Phone_Is_Empty()
    {
        var input = CreateValidInput(phone: "");

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Phone)
              .WithErrorMessage("Phone is required");
    }

    [Fact]
    public void Should_Have_Error_When_Address_Is_Null()
    {
        var input = new RegisterCustomerInput
        {
            Name = "João Silva",
            Document = "12345678900",
            Email = "joao@email.com",
            Phone = "44999999999",
            Address = null!
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Address)
              .WithErrorMessage("Address is required");
    }

    [Fact]
    public void Should_Have_Error_When_Street_Is_Invalid()
    {
        var address = CreateValidAddress(street: "A");

        var input = CreateValidInput(address: address);

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor("Street")
              .WithErrorMessage("Street must have at least 3 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Number_Is_Too_Long()
    {

        var address = CreateValidAddress(number: "1234567891011");

        var input = CreateValidInput(address: address);

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor("Number")
              .WithErrorMessage("Number too long");
    }

    [Fact]
    public void Should_Have_Error_When_Neighborhood_Is_Invalid()
    {
        var address = CreateValidAddress(neighborhood: "A");

        var input = CreateValidInput(address: address);

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor("Neighborhood")
              .WithErrorMessage("Neighborhood must have at least 2 characters");
    }

    [Fact]
    public void Should_Have_Error_When_City_Is_Invalid()
    {
        var address = CreateValidAddress(city: "A");

        var input = CreateValidInput(address: address);

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor("City")
              .WithErrorMessage("City must have at least 2 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Cep_Is_Empty()
    {
        var address = CreateValidAddress(cep: "");

        var input = CreateValidInput(address: address);

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor("Cep")
              .WithErrorMessage("Cep is required");
    }
}