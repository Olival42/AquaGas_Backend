using Xunit;
using AquaGas.Shared.Results;
using AquaGas.Shared.Errors;

namespace AquaGas.Tests.Shared.Results
{
    public class ResultTTests
    {
        [Fact]
        public void Success_Should_Set_Success_True()
        {
            var result = Result<string>.Success("ok");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Success_Should_Set_Value()
        {
            var result = Result<string>.Success("ok");

            Assert.Equal("ok", result.Value);
        }

        [Fact]
        public void Success_Should_Not_Have_Errors()
        {
            var result = Result<string>.Success("ok");

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Fail_Should_Set_Success_False()
        {
            var error = new Error("code", "message");

            var result = Result<string>.Fail(error);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Fail_Should_Set_Errors()
        {
            var error = new Error("code", "message");

            var result = Result<string>.Fail(error);

            Assert.Single(result.Errors);
        }

        [Fact]
        public void Fail_Should_Not_Set_Value()
        {
            var error = new Error("code", "message");

            var result = Result<string>.Fail(error);

            Assert.Null(result.Value);
        }

        [Fact]
        public void Fail_Should_Set_Default_Value()
        {
            var error = new Error("code", "message");

            var result = Result<int>.Fail(error);

            Assert.Equal(default, result.Value);
        }

        [Fact]
        public void Combine_Should_Return_Success_When_All_Success()
        {
            var r1 = Result.Success();
            var r2 = Result.Success();

            var result = Result.Combine(r1, r2);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Combine_Should_Return_Validation_Errors()
        {
            var r1 = Result.Fail(Error.Validation("msg", "field"));
            var r2 = Result.Success();

            var result = Result.Combine(r1, r2);

            Assert.True(result.IsFailure);
            Assert.True(result.IsValidationError);
        }

        [Fact]
        public void Combine_Should_Return_Critical_Error()
        {
            var r1 = Result.Fail(new Error("ERROR", "critical"));
            var r2 = Result.Fail(Error.Validation("msg", "field"));

            var result = Result.Combine(r1, r2);

            Assert.True(result.IsFailure);
            Assert.Single(result.Errors);
            Assert.Equal("ERROR", result.Errors[0].Code);
        }
    }
}