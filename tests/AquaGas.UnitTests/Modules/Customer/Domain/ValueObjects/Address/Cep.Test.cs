using Xunit;
using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Address;

public class CepTests
{
    [Fact]
    public void Should_Create_When_Cep_Is_Valid()
    {
        var input = "87050-000";

        var result = Cep.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("87050000", result.Value!.Value);
    }

    [Fact]
    public void Should_Normalize_Removing_Non_Digits()
    {
        var input = "87.050-000";

        var result = Cep.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("87050000", result.Value!.Value);
    }

    [Fact]
    public void Should_Fail_When_Cep_Is_Null_Or_Empty()
    {
        var result1 = Cep.Create(null!);
        var result2 = Cep.Create("");
        var result3 = Cep.Create("   ");

        Assert.True(result1.IsFailure);
        Assert.True(result2.IsFailure);
        Assert.True(result3.IsFailure);
    }

    [Fact]
    public void Should_Fail_When_Cep_Is_Invalid_Length()
    {
        var input = "12345";

        var result = Cep.Create(input);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Should_Fail_When_Cep_Has_Letters()
    {
        var input = "ABCDE123";

        var result = Cep.Create(input);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("87050000")]
    [InlineData("87050-000")]
    [InlineData("87.050-000")]
    public void Should_Accept_Valid_Formats(string input)
    {
        var result = Cep.Create(input);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Should_Be_Equal_When_Values_Are_Same()
    {
        var cep1 = Cep.Create("87050-000").Value;
        var cep2 = Cep.Create("87050000").Value;

        Assert.Equal(cep1, cep2);
    }

    [Fact]
    public void Should_Not_Be_Equal_When_Values_Are_Different()
    {
        var cep1 = Cep.Create("87050-000").Value;
        var cep2 = Cep.Create("01001-000").Value;

        Assert.NotEqual(cep1, cep2);
    }

    [Fact]
    public void Should_Have_Same_HashCode_When_Equal()
    {
        var cep1 = Cep.Create("87050-000").Value!;
        var cep2 = Cep.Create("87050000").Value!;

        Assert.Equal(cep1.GetHashCode(), cep2.GetHashCode());
    }
}