using Xunit;
using AquaGas.Shared.Results;
using AquaGas.Shared.Responses;
using AquaGas.Shared.Errors;

namespace AquaGas.Tests.Shared.Responses;

public class ApiResponseMapperTests
{
    [Fact]
    public void ToApiResponse_Should_Map_Success()
    {
        var result = Result<string>.Success("dados");

        var response = result.ToApiResponse();

        Assert.True(response.Success);
        Assert.Equal("dados", response.Data);
        Assert.Null(response.Error);
    }

    [Fact]
    public void ToApiResponse_Should_Map_Simple_Failure()
    {
        var result = Result<string>.Fail(
            new Error("ERROR", "Something wrong")
        );

        var response = result.ToApiResponse();

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("ERROR", response.Error!.Code);
        Assert.Equal("Something wrong", response.Error.Message);
        Assert.Null(response.Error.Details);
    }

    [Fact]
    public void ToApiResponse_Should_Group_Validation_Errors_By_Field()
    {
        var result = Result<string>.Fail(
            new Error("VALIDATION_ERROR", "Name required") { Field = "Name" },
            new Error("VALIDATION_ERROR", "Email invalid") { Field = "Email" }
        );

        var response = result.ToApiResponse();

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("VALIDATION_ERROR", response.Error!.Code);

        var details = response.Error.Details as List<DataErrors>;

        Assert.NotNull(details);
        Assert.Equal(2, details.Count);

        Assert.Contains(details, d =>
            d.Field == "Name" && d.Message.Contains("Name required"));

        Assert.Contains(details, d =>
            d.Field == "Email" && d.Message.Contains("Email invalid"));
    }

    [Fact]
    public void ToApiResponse_Should_Group_Multiple_Errors_Same_Field()
    {
        var result = Result<string>.Fail(
            new Error("VALIDATION_ERROR", "Name required") { Field = "Name" },
            new Error("VALIDATION_ERROR", "Name too short") { Field = "Name" }
        );

        var response = result.ToApiResponse();

        var details = response.Error!.Details as List<DataErrors>;

        Assert.Single(details!);

        var nameErrors = details!.First(d => d.Field == "Name");

        Assert.Equal(2, nameErrors.Message.Count);
        Assert.Contains("Name required", nameErrors.Message);
        Assert.Contains("Name too short", nameErrors.Message);
    }

    [Fact]
    public void ToApiResponse_Should_Fallback_To_First_Error_When_Not_Validation()
    {
        var result = Result<string>.Fail(
            new Error("ERROR_1", "First error"),
            new Error("ERROR_2", "Second error")
        );

        var response = result.ToApiResponse();

        Assert.False(response.Success);
        Assert.Equal("ERROR_1", response.Error!.Code);
        Assert.Equal("First error", response.Error.Message);
        Assert.Null(response.Error.Details);
    }
}