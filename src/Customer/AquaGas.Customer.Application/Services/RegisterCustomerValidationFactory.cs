using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Domain.ValueObjects.Address;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Customer.Application.Services;

public static class RegisterCustomerValidationFactory
{
    public static Result<RegisterCustomerValidated> Combine(RegisterCustomerInput data)
    {
        if (data is null)
            return Result<RegisterCustomerValidated>.Fail(
                Error.Validation("Input is required", "Input")
            );

        if (data.Address is null)
            return Result<RegisterCustomerValidated>.Fail(
                Error.Validation("Address is required", "Address")
            );

        var name = CustomerName.Create(data.Name);
        var document = Document.Create(data.Document);
        var email = Email.Create(data.Email);
        var phone = Phone.Create(data.Phone);
        var cep = Cep.Create(data.Address.Cep);

        var result = Result.Combine(
            name, document, email, phone, cep
        );

        if (result.IsFailure)
            return Result<RegisterCustomerValidated>.Fail(result.Errors.ToArray());

        return Result<RegisterCustomerValidated>.Success(
            new RegisterCustomerValidated
            {
                Name = name.Value!,
                Document = document.Value!,
                Email = email.Value!,
                Phone = phone.Value!,
                Address = new RegisterAddressValidated
                {
                    Street = data.Address.Street,
                    Neighborhood = data.Address.Neighborhood,
                    Number = data.Address.Number,
                    Complement = data.Address.Complement,
                    City = data.Address.City,
                    Cep = cep.Value!
                }
            }
        );
    }
}