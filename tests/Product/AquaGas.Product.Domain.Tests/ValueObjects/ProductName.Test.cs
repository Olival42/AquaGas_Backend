using AquaGas.Product.Domain.ValueObjects;
using Xunit;

public class ProductNameTests
{
    [Fact]
    public void Create_Should_Return_Success_When_Name_Is_Valid()
    {
        var result = ProductName.Create("Agua Mineral");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Agua Mineral", result.Value!.Value);
    }

    [Fact]
    public void Create_Should_Trim_Name()
    {
        var result = ProductName.Create("   Agua Mineral   ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Agua Mineral", result.Value!.Value);
    }

    [Fact]
    public void Create_Should_Return_Failure_When_Name_Is_Null()
    {
        var result = ProductName.Create(null!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors,
            e => e.Message == "Product name is required");
    }

    [Fact]
    public void Create_Should_Return_Failure_When_Name_Is_Empty()
    {
        var result = ProductName.Create("");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors,
            e => e.Message == "Product name is required");
    }

    [Fact]
    public void Create_Should_Return_Failure_When_Name_Is_Whitespace()
    {
        var result = ProductName.Create("     ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors,
            e => e.Message == "Product name is required");
    }

    [Fact]
    public void Create_Should_Return_Failure_When_Name_Has_Less_Than_3_Characters()
    {
        var result = ProductName.Create("Ab");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors,
            e => e.Message == "Product name must have at least 3 characters");
    }

    [Fact]
    public void Equals_Should_Return_True_When_Values_Are_Equal()
    {
        var name1 = ProductName.Create("Agua").Value!;
        var name2 = ProductName.Create("Agua").Value!;

        Assert.True(name1.Equals(name2));
    }

    [Fact]
    public void Equals_Should_Return_True_Ignoring_Case()
    {
        var name1 = ProductName.Create("Agua").Value!;
        var name2 = ProductName.Create("agua").Value!;

        Assert.True(name1.Equals(name2));
    }

    [Fact]
    public void Equals_Should_Return_False_When_Values_Are_Different()
    {
        var name1 = ProductName.Create("Agua").Value!;
        var name2 = ProductName.Create("Gas").Value!;

        Assert.False(name1.Equals(name2));
    }

    [Fact]
    public void Equals_Should_Return_False_When_Object_Is_Null()
    {
        var name = ProductName.Create("Agua").Value!;

        Assert.False(name.Equals(null));
    }

    [Fact]
    public void Equals_Should_Return_False_When_Object_Is_Different_Type()
    {
        var name = ProductName.Create("Agua").Value!;

        Assert.False(name.Equals("Agua"));
    }

    [Fact]
    public void Operator_Equals_Should_Return_True_When_Values_Are_Equal()
    {
        var name1 = ProductName.Create("Agua").Value!;
        var name2 = ProductName.Create("Agua").Value!;

        Assert.True(name1 == name2);
    }

    [Fact]
    public void Operator_Equals_Should_Ignore_Case()
    {
        var name1 = ProductName.Create("Agua").Value!;
        var name2 = ProductName.Create("agua").Value!;

        Assert.True(name1 == name2);
    }

    [Fact]
    public void Operator_NotEquals_Should_Return_True_When_Values_Are_Different()
    {
        var name1 = ProductName.Create("Agua").Value!;
        var name2 = ProductName.Create("Gas").Value!;

        Assert.True(name1 != name2);
    }

    [Fact]
    public void Operator_Equals_Should_Handle_Null_Values()
    {
        ProductName? name1 = null;
        ProductName? name2 = null;

        Assert.True(name1 == name2);
    }

    [Fact]
    public void Operator_NotEquals_Should_Handle_One_Null_Value()
    {
        var name1 = ProductName.Create("Agua").Value!;
        ProductName? name2 = null;

        Assert.True(name1 != name2);
    }

    [Fact]
    public void GetHashCode_Should_Be_Equal_For_Same_Values()
    {
        var name1 = ProductName.Create("Agua").Value!;
        var name2 = ProductName.Create("agua").Value!;

        Assert.Equal(name1.GetHashCode(), name2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Should_Be_Different_For_Different_Values()
    {
        var name1 = ProductName.Create("Agua").Value!;
        var name2 = ProductName.Create("Gas").Value!;

        Assert.NotEqual(name1.GetHashCode(), name2.GetHashCode());
    }

    [Fact]
    public void ToString_Should_Return_Value()
    {
        var name = ProductName.Create("Agua Mineral").Value!;

        Assert.Equal("Agua Mineral", name.ToString());
    }
}