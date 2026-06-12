namespace AquaGas.Customer.Application.Mappings;

using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;
using Mapster;
using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Domain.Models;
using AquaGas.Customer.Application.Dtos.Responses;

public static class CustomerMapping
{
    private static readonly Lock Sync = new();
    private static bool _globalRegistered;
    public static void Register(TypeAdapterConfig? config = null)
    {
        if (config is not null)
        {
            Apply(config);
            return;
        }

        lock (Sync)
        {
            if (_globalRegistered)
                return;

            Apply(TypeAdapterConfig.GlobalSettings);
            _globalRegistered = true;
        }
    }

    private static void Apply(TypeAdapterConfig config)
    {
        config.NewConfig<RegisterCustomerValidated, CustomerEntity>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Document, src => src.Document)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.Phone, src => src.Phone)
            .Ignore(dest => dest.Addresses);

        config.NewConfig<RegisterAddressValidated, Address>()
            .Map(dest => dest.Street, src => src.Street)
            .Map(dest => dest.Neighborhood, src => src.Neighborhood)
            .Map(dest => dest.Number, src => src.Number)
            .Map(dest => dest.Complement, src => src.Complement)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.Cep, src => src.Cep);

        config.NewConfig<CustomerEntity, CustomerResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Document, src => src.Document.Value)
            .Map(dest => dest.TypeDocument, src => src.Document.Type)
            .Map(dest => dest.Email, src => src.Email.Value)
            .Map(dest => dest.Phone, src => src.Phone.Value)
            .Map(dest => dest.Address, src => src.Addresses.FirstOrDefault())
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        config.NewConfig<Address, AddressResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Street, src => src.Street)
            .Map(dest => dest.Neighborhood, src => src.Neighborhood)
            .Map(dest => dest.Number, src => src.Number)
            .Map(dest => dest.Complement, src => src.Complement)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.Cep, src => src.Cep.Value);
    }
}
