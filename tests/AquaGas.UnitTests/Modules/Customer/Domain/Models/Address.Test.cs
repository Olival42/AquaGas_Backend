using Xunit;
using AquaGas.Api.Modules.Customer.Domain.Models;
using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Address;

public class AddressTests
{
    private Cep CreateValidCep()
    {
        return Cep.Create("87000000").Value!;
    }

    [Fact]
    public void Should_Create_Address_Successfully()
    {
        var cep = CreateValidCep();

        var address = new Address(
            Guid.NewGuid(),
            "Rua A",
            "Centro",
            "123",
            "Apto 1",
            "Maringá",
            cep
        );

        Assert.Equal("Rua A", address.Street);
        Assert.Equal("Centro", address.Neighborhood);
        Assert.Equal("123", address.Number);
        Assert.Equal("Apto 1", address.Complement);
        Assert.Equal("Maringá", address.City);
        Assert.Equal(cep, address.Cep);
        Assert.NotEqual(Guid.Empty, address.Id);
    }

    [Fact]
    public void Should_Update_All_Fields()
    {
        var cep = CreateValidCep();

        var address = new Address(
            Guid.NewGuid(),
            "Rua A",
            "Centro",
            "123",
            null,
            "Maringá",
            cep
        );

        var newCep = Cep.Create("88000000").Value;

        address.Update(
            "Rua B",
            "Zona 7",
            "456",
            "Casa",
            "Curitiba",
            newCep
        );

        Assert.Equal("Rua B", address.Street);
        Assert.Equal("Zona 7", address.Neighborhood);
        Assert.Equal("456", address.Number);
        Assert.Equal("Casa", address.Complement);
        Assert.Equal("Curitiba", address.City);
        Assert.Equal(newCep, address.Cep);
    }

    [Fact]
    public void Should_Not_Update_When_Values_Are_Null()
    {
        var cep = CreateValidCep();

        var address = new Address(
            Guid.NewGuid(),
            "Rua A",
            "Centro",
            "123",
            "Apto 1",
            "Maringá",
            cep
        );

        address.Update(
            null,
            null,
            null,
            null,
            null,
            null
        );

        Assert.Equal("Rua A", address.Street);
        Assert.Equal("Centro", address.Neighborhood);
        Assert.Equal("123", address.Number);
        Assert.Equal("Apto 1", address.Complement);
        Assert.Equal("Maringá", address.City);
        Assert.Equal(cep, address.Cep);
    }

    [Fact]
    public void Should_Update_Only_Some_Fields()
    {
        var cep = CreateValidCep();

        var address = new Address(
            Guid.NewGuid(),
            "Rua A",
            "Centro",
            "123",
            null,
            "Maringá",
            cep
        );

        address.Update(
            "Rua Nova",
            null,
            null,
            null,
            "Londrina",
            null
        );

        Assert.Equal("Rua Nova", address.Street);
        Assert.Equal("Centro", address.Neighborhood);
        Assert.Equal("123", address.Number);
        Assert.Equal("Londrina", address.City);
    }
}