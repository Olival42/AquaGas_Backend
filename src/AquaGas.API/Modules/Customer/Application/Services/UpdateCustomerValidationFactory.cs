using System.Text.RegularExpressions;
using AquaGas.Api.Modules.Customer.Application.Dtos.Requests;
using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Address;
using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Customer;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Application.Validation;

namespace AquaGas.API.Modules.Customer.Application.Services;

public static class UpdateCustomerValidationFactory
{
    public static Result<UpdateCustomerValidated> Combine(UpdateCustomerInput data)
    {
        var errors = new List<Error>();

        CustomerName? name = null;
        Document? document = null;
        Email? email = null;
        Phone? phone = null;

        if (data.Name is not null)
        {
            var result = CustomerName.Create(data.Name);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else name = result.Value;
        }

        if (data.Document is not null)
        {
            var result = Document.Create(data.Document);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else document = result.Value;
        }

        if (data.Email is not null)
        {
            var result = Email.Create(data.Email);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else email = result.Value;
        }

        if (data.Phone is not null)
        {
            var result = Phone.Create(data.Phone);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else phone = result.Value;
        }

        UpdateAddressValidated? validatedAddress = null;

        if (data.Address is not null)
        {
            if (!data.Address.AddressId.HasValue || data.Address.AddressId == Guid.Empty)
            {
                errors.Add(Error.Validation("AddressId is required and must be valid", "AddressId"));
            }
            else
            {
                validatedAddress = new UpdateAddressValidated
                {
                    Id = data.Address.AddressId.Value
                };

                SetIfValidOptional(data.Address.Street, "Street", 3, errors, v => validatedAddress.Street = v);

                SetIfValidOptional(data.Address.Neighborhood, "Neighborhood", 2, errors, v => validatedAddress.Neighborhood = v);

                SetIfValidOptional(data.Address.City, "City", 2, errors, v => validatedAddress.City = v);

                if (data.Address.Number is not null)
                {
                    var value = data.Address.Number.Trim();

                    if (string.IsNullOrWhiteSpace(value))
                        errors.Add(Error.Validation("Number cannot be empty", "Number"));
                    else if (value.Length > 10)
                        errors.Add(Error.Validation("Number too long", "Number"));
                    else
                        validatedAddress.Number = value;
                }

                if (data.Address.Complement is not null)
                {
                    var value = Regex.Replace(data.Address.Complement.Trim(), @"\s+", " ");

                    if (value.Length > 100)
                        errors.Add(Error.Validation("Complement too long", "Complement"));
                    else
                        validatedAddress.Complement = value;
                }

                if (data.Address.Cep is not null)
                {
                    var result = Cep.Create(data.Address.Cep);
                    if (result.IsFailure) errors.AddRange(result.Errors);
                    else validatedAddress.Cep = result.Value;
                }
            }
        }

        if (errors.Any())
            return Result<UpdateCustomerValidated>.Fail(errors.ToArray());

        return Result<UpdateCustomerValidated>.Success(
            new UpdateCustomerValidated
            {
                Name = name,
                Document = document,
                Email = email,
                Phone = phone,
                Address = validatedAddress
            }
        );
    }

    private static void SetIfValidOptional(
        string? input,
        string field,
        int min,
        List<Error> errors,
        Action<string> setter
    )
    {
        if (input is null) return;

        var value = StringValidator.Validate(input, field, min, errors);

        if (value is not null)
            setter(value);
    }
}