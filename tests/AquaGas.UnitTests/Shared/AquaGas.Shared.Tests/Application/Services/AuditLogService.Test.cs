using Xunit;
using Moq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using AquaGas.Shared.Application.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Domain.Models;
using AquaGas.Application.Services;

public class AuditLogServiceTests
{
    [Fact]
    public async Task Should_Create_And_Save_AuditLog()
    {
        var repoMock = new Mock<IAuditLogRepository>();

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["User-Agent"] = "test-agent";

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new AuditLogService(repoMock.Object, httpMock.Object);

        var oldObj = new { Name = "Old" };
        var newObj = new { Name = "New" };

        await service.LogAsync(
            Guid.NewGuid(),
            "user",
            AuditAction.CREATE,
            "User",
            Guid.NewGuid(),
            oldObj,
            newObj
        );

        repoMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task Should_Serialize_Old_And_New_Values()
    {
        var repoMock = new Mock<IAuditLogRepository>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

        AuditLog captured = null!;

        repoMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => captured = log)
            .Returns(Task.CompletedTask);

        var service = new AuditLogService(repoMock.Object, httpMock.Object);

        var oldObj = new { Name = "Old" };

        await service.LogAsync(
            Guid.NewGuid(),
            "user",
            AuditAction.UPDATE,
            "User",
            Guid.NewGuid(),
            oldObj,
            null
        );

        Assert.Contains("Old", captured.OldValues);
    }

    [Fact]
    public async Task Should_Capture_Ip_And_UserAgent()
    {
        var repoMock = new Mock<IAuditLogRepository>();

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        context.Request.Headers["User-Agent"] = "test-agent";

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(x => x.HttpContext).Returns(context);

        AuditLog captured = null!;

        repoMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => captured = log)
            .Returns(Task.CompletedTask);

        var service = new AuditLogService(repoMock.Object, httpMock.Object);

        await service.LogAsync(null, null, AuditAction.CREATE, "User", null);

        Assert.Equal("127.0.0.1", captured.IpAddress);
        Assert.Equal("test-agent", captured.UserAgent);
    }
}