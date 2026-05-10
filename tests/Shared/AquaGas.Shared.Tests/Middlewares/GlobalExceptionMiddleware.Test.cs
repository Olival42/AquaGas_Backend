using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AquaGas.Shared.Middlewares;
using AquaGas.Shared.Responses;

namespace AquaGas.Tests.Shared.Middlewares
{
    public class GlobalExceptionMiddlewareTests
    {
        private readonly Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock;

        public GlobalExceptionMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        }

        [Fact]
        public async Task InvokeAsync_Should_Return_400_When_ArgumentException()
        {
            var context = new DefaultHttpContext();
            var middleware = new GlobalExceptionMiddleware(
                (innerHttpContext) => throw new ArgumentException("Invalid"),
                _loggerMock.Object);

            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_Should_Return_401_When_UnauthorizedException()
        {
            var context = new DefaultHttpContext();
            var middleware = new GlobalExceptionMiddleware(
                (innerHttpContext) => throw new UnauthorizedAccessException("No access"),
                _loggerMock.Object);

            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_Should_Return_500_When_GenericException()
        {
            var context = new DefaultHttpContext();
            var middleware = new GlobalExceptionMiddleware(
                (innerHttpContext) => throw new Exception("Boom"),
                _loggerMock.Object);

            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_Should_Write_ApiResponse()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var middleware = new GlobalExceptionMiddleware(
                (innerHttpContext) => throw new Exception("Boom"),
                _loggerMock.Object);

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

            var response = JsonSerializer.Deserialize<ApiResponse<object>>(body, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.False(response!.Success);
            Assert.NotNull(response.Error);
        }
    }
}