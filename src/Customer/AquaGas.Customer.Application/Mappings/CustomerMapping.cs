namespace AquaGas.Customer.Application.Mappings;

using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;
using Mapster;
using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Domain.Models;
using AquaGas.Customer.Application.Dtos.Responses;

public static class CustomerMapping
{
    public static void Register()
    {
        TypeAdapterConfig<RegisterCustomerValidated, CustomerEntity>
            .NewConfig()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Document, src => src.Document)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.Phone, src => src.Phone)
            .Ignore(dest => dest.Addresses);

        TypeAdapterConfig<RegisterAddressValidated, Address>
            .NewConfig()
            .Map(dest => dest.Street, src => src.Street)
            .Map(dest => dest.Neighborhood, src => src.Neighborhood)
            .Map(dest => dest.Number, src => src.Number)
            .Map(dest => dest.Complement, src => src.Complement)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.Cep, src => src.Cep);

        TypeAdapterConfig<CustomerEntity, CustomerResponse>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Document, src => src.Document.Value)
            .Map(dest => dest.TypeDocument, src => src.Document.Type)
            .Map(dest => dest.Email, src => src.Email.Value)
            .Map(dest => dest.Phone, src => src.Phone.Value)
            .Map(dest => dest.Address, src => src.Addresses.FirstOrDefault())
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        TypeAdapterConfig<Address, AddressResponse>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Street, src => src.Street)
            .Map(dest => dest.Neighborhood, src => src.Neighborhood)
            .Map(dest => dest.Number, src => src.Number)
            .Map(dest => dest.Complement, src => src.Complement)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.Cep, src => src.Cep.Value);
    }
}