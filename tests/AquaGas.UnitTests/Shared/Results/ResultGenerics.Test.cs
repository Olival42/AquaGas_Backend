using Xunit;
using AquaGas.Api.Shared.Results;
using AquaGas.Api.Shared.Errors;

namespace AquaGas.Tests.Shared.Results
{
    public class ResultGenericsTTests
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

            Assert.NotNull(result.Errors);
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
        public void Fail_Should_Accept_Multiple_Errors()
        {
            var error1 = new Error("code1", "message1");
            var error2 = new Error("code2", "message2");

            var result = Result<string>.Fail(error1, error2);

            Assert.Equal(2, result.Errors!.Count);
        }

        [Fact]
        public void Fail_Should_Set_Default_For_Value_Type()
        {
            var error = new Error("code", "message");

            var result = Result<int>.Fail(error);

            Assert.Equal(default, result.Value);
        }
    }
}