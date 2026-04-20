using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using AquaGas.Api.Modules.Auth.Infrastructure.Services;

public class UserContextServiceTests
{
    [Fact]
    public void Should_Return_UserId_From_Claims()
    {
        var userId = "123";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new UserContextService(accessorMock.Object);

        var result = service.GetUserId();

        Assert.Equal(userId, result);
    }

    [Fact]
    public void Should_Return_Null_When_No_HttpContext()
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new UserContextService(accessorMock.Object);

        var result = service.GetUserId();

        Assert.Null(result);
    }

    [Fact]
    public void Should_Return_Null_When_No_Claim()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new UserContextService(accessorMock.Object);

        var result = service.GetUserId();

        Assert.Null(result);
    }
}