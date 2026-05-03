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

    [Fact]
    public void Should_Return_UserName_From_Claims()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, "admin")
                })
            )
        };

        var service = new UserContextService(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var result = service.GetUserName();

        Assert.True(result.IsSuccess);
        Assert.Equal("admin", result.Value);
    }

    [Fact]
    public void Should_Fail_GetUserName_When_No_HttpContext()
    {
        var service = new UserContextService(new HttpContextAccessor
        {
            HttpContext = null
        });

        var result = service.GetUserName();

        Assert.True(result.IsFailure);
        Assert.Equal("Missing UserName in token", result.Errors[0].Message);
    }

    [Fact]
    public void Should_Fail_GetUserName_When_No_Claim()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        var service = new UserContextService(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var result = service.GetUserName();

        Assert.True(result.IsFailure);
        Assert.Equal("Missing UserName in token", result.Errors[0].Message);
    }

    [Fact]
    public void Should_Fail_When_UserName_Is_Empty()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, "")
                })
            )
        };

        var service = new UserContextService(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var result = service.GetUserName();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Should_Fail_When_UserName_Is_Whitespace()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, "   ")
                })
            )
        };

        var service = new UserContextService(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var result = service.GetUserName();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Should_Use_Correct_Claim_For_UserName()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Email, "email@email.com"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
                })
            )
        };

        var service = new UserContextService(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var result = service.GetUserName();

        Assert.True(result.IsSuccess);
        Assert.Equal("admin", result.Value);
    }
}