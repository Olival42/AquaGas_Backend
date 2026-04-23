using Xunit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class UserContextServiceTests
{
    [Fact]
    public void Should_Return_UserId_From_Claims()
    {
        var userId = Guid.NewGuid();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                })
            )
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var service = new UserContextService(accessor);

        var result = service.GetUserId();

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value);
    }

    [Fact]
    public void Should_Fail_When_No_HttpContext()
    {
        var service = new UserContextService(new HttpContextAccessor
        {
            HttpContext = null
        });

        var result = service.GetUserId();

        Assert.True(result.IsFailure);
        Assert.Equal("Missing UserId in token", result.Errors[0].Message);
    }

    [Fact]
    public void Should_Fail_When_No_Claim()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        var service = new UserContextService(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var result = service.GetUserId();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Should_Fail_When_Invalid_Guid()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "invalid-guid")
                })
            )
        };

        var service = new UserContextService(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var result = service.GetUserId();

        Assert.True(result.IsFailure);
    }
}