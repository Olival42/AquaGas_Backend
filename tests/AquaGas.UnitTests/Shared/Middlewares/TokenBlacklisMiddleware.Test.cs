using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using AquaGas.Api.Shared.Middlewares;
using AquaGas.Api.Shared.Infrastructure.TokenBlacklist;
using AquaGas.Api.Shared.Responses;
using System.Security.Claims;

namespace AquaGas.Tests.Shared.Middlewares
{
    public class TokenBlacklistMiddlewareTests
    {
        [Fact]
        public async Task Should_Call_Next_When_User_Not_Authenticated()
        {
            var context = new DefaultHttpContext();

            var blacklistMock = new Mock<ITokenBlacklistService>();

            var nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new TokenBlacklistMiddleware(next);

            await middleware.InvokeAsync(context, blacklistMock.Object);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task Should_Call_Next_When_Token_Not_Blacklisted()
        {
            var context = CreateAuthenticatedContext("valid-token");

            var blacklistMock = new Mock<ITokenBlacklistService>();
            blacklistMock
                .Setup(x => x.IsBlacklistedAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new TokenBlacklistMiddleware(next);

            await middleware.InvokeAsync(context, blacklistMock.Object);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task Should_Return_401_When_Token_Blacklisted()
        {
            var context = CreateAuthenticatedContext("revoked-token");
            context.Response.Body = new MemoryStream();

            var blacklistMock = new Mock<ITokenBlacklistService>();
            blacklistMock
                .Setup(x => x.IsBlacklistedAsync("revoked-token"))
                .ReturnsAsync(true);

            var middleware = new TokenBlacklistMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context, blacklistMock.Object);

            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact]
        public async Task Should_Return_ApiResponse_When_Token_Blacklisted()
        {
            var context = CreateAuthenticatedContext("revoked-token");
            context.Response.Body = new MemoryStream();

            var blacklistMock = new Mock<ITokenBlacklistService>();
            blacklistMock
                .Setup(x => x.IsBlacklistedAsync("revoked-token"))
                .ReturnsAsync(true);

            var middleware = new TokenBlacklistMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context, blacklistMock.Object);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(context.Response.Body).ReadToEndAsync();

            var response = JsonSerializer.Deserialize<ApiResponse<ErrorResponse>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.False(response!.Success);
            Assert.Equal("UNAUTHORIZED", response.Error!.Code);
        }

        private static DefaultHttpContext CreateAuthenticatedContext(string token)
        {
            var context = new DefaultHttpContext();

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "user")
            }, "TestAuth");

            context.User = new ClaimsPrincipal(identity);

            context.Request.Headers["Authorization"] = $"Bearer {token}";

            return context;
        }
    }
}