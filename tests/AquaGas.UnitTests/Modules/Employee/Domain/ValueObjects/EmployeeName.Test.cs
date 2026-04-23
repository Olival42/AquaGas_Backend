using Xunit;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;

public class EmployeeNameTests
{
    [Fact]
    public void Should_Create_When_Name_Is_Valid()
    {
        var result = EmployeeName.Create("João");

        Assert.True(result.IsSuccess);
        Assert.Equal("João", result.Value!.Value);
    }

    [Fact]
    public void Should_Trim_Name()
    {
        var result = EmployeeName.Create("   João   ");

        Assert.True(result.IsSuccess);
        Assert.Equal("João", result.Value!.Value);
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var result = EmployeeName.Create("");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Null()
    {
        var result = EmployeeName.Create(null!);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Whitespace()
    {
        var result = EmployeeName.Create("   ");

        Assert.False(result.IsSuccess);
    }
}