using Xunit;
using AquaGas.Api.Shared.Responses;

namespace AquaGas.Tests.Shared.Responses
{
    public class ApiResponseTests
    {
        [Fact]
        public void Ok_Should_Set_Success_True()
        {
            var response = ApiResponse<string>.Ok("dados");

            Assert.True(response.Success);
        }

        [Fact]
        public void Ok_Should_Set_Data()
        {
            var response = ApiResponse<string>.Ok("dados");

            Assert.Equal("dados", response.Data);
        }

        [Fact]
        public void Ok_Should_Not_Set_Error()
        {
            var response = ApiResponse<string>.Ok("dados");

            Assert.Null(response.Error);
        }

        [Fact]
        public void Ok_Should_Set_Timestamp()
        {
            var response = ApiResponse<string>.Ok("dados");

            Assert.NotEqual(default, response.Timestamp);
        }

        [Fact]
        public void Fail_Should_Set_Success_False()
        {
            var error = new ErrorResponse("code", "message");

            var response = ApiResponse<string>.Fail(error);

            Assert.False(response.Success);
        }

        [Fact]
        public void Fail_Should_Set_Error()
        {
            var error = new ErrorResponse("code", "message");

            var response = ApiResponse<string>.Fail(error);

            Assert.NotNull(response.Error);
        }

        [Fact]
        public void Fail_Should_Not_Set_Data()
        {
            var error = new ErrorResponse("code", "message");

            var response = ApiResponse<string>.Fail(error);

            Assert.Null(response.Data);
        }

        [Fact]
        public void Fail_Should_Set_Timestamp()
        {
            var error = new ErrorResponse("code", "message");

            var response = ApiResponse<string>.Fail(error);

            Assert.NotEqual(default, response.Timestamp);
        }

        [Fact]
        public void Ok_Should_Set_Current_Utc_Timestamp()
        {
            var before = DateTimeOffset.UtcNow;

            var response = ApiResponse<string>.Ok("dados");

            var after = DateTimeOffset.UtcNow;

            Assert.InRange(response.Timestamp, before, after);
        }
    }
}