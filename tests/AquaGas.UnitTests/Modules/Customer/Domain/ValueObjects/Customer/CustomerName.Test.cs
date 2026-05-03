using Xunit;
using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Customer;

public class CustomerNameTests
{
    [Fact]
    public void Should_Create_When_Name_Is_Valid()
    {
        var input = "João Silva";

        var result = CustomerName.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("João Silva", result.Value!.Value);
    }

    [Fact]
    public void Should_Normalize_Multiple_Spaces()
    {
        var input = "  João    Silva   ";

        var result = CustomerName.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("João Silva", result.Value!.Value);
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Null()
    {
        var result = CustomerName.Create(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message == "Name is required");
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var result = CustomerName.Create("   ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message == "Name cannot be empty");
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Too_Short()
    {
        var result = CustomerName.Create("Jo");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("at least 3 characters"));
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        var input = new string('A', 151);

        var result = CustomerName.Create(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("at most 150 characters"));
    }

    [Theory]
    [InlineData("Maria")]
    [InlineData("Ana Clara")]
    [InlineData("José da Silva")]
    public void Should_Accept_Valid_Names(string input)
    {
        var result = CustomerName.Create(input);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Should_Keep_Accented_Characters()
    {
        var input = "José Álvarez";

        var result = CustomerName.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("José Álvarez", result.Value!.Value);
    }
}