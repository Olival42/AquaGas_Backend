using Xunit;
using AquaGas.Api.Shared.Results;
using AquaGas.Api.Shared.Responses;
using AquaGas.Api.Shared.Errors;

namespace AquaGas.Tests.Shared.Responses
{
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
        public void ToApiResponse_Should_Map_Failure()
        {
            var error = new Error("ERROR", "Something wrong");
            var result = Result<string>.Fail(error);

            var response = result.ToApiResponse();

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("ERROR", response.Error!.Code);
        }

        [Fact]
        public void ToApiResponse_Should_Map_Validation_Errors()
        {
            var error1 = new Error("VALIDATION_ERROR", "Name required");
            var error2 = new Error("VALIDATION_ERROR", "Email invalid");

            var result = Result<string>.Fail(error1, error2);

            var response = result.ToApiResponse();

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal("VALIDATION_ERROR", response.Error!.Code);
            Assert.Equal(2, response.Error.Details!.Count);
        }

        [Fact]
        public void ToApiResponse_Should_Use_First_Error_When_Not_Validation()
        {
            var error1 = new Error("ERROR_1", "First error");
            var error2 = new Error("ERROR_2", "Second error");

            var result = Result<string>.Fail(error1, error2);

            var response = result.ToApiResponse();

            Assert.Equal("ERROR_1", response.Error!.Code);
            Assert.Equal("First error", response.Error!.Message);
        }
    }
}