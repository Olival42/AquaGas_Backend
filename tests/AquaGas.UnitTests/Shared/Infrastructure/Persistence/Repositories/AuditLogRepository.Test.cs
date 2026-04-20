using Microsoft.EntityFrameworkCore;
using Xunit;
using AquaGas.Api.Shared.Infrastructure.Persistence.Repositories;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.API.Shared.Domain.Models;
using AquaGas.API.Shared.Domain.Enums;

namespace AquaGas.Tests.Shared.Infrastructure.Persistence.Repositories
{
    public class AuditLogRepositoryTests
    {
        [Fact]
        public async Task AddAsync_Should_Save_Log()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new AppDbContext(options);
            var repository = new AuditLogRepository(context);

            var log = AuditLog.Create(
                userId: Guid.NewGuid(),
                userName: "Joao123",
                action: AuditAction.CREATE,
                entityType: "User",
                entityId: Guid.NewGuid(),
                oldValues: null,
                newValues: "{\"name\":\"Joao\"}",
                ipAddress: "127.0.0.1",
                userAgent: "xUnit",
                correlationId: Guid.NewGuid()
            );

            await repository.AddAsync(log);

            var saved = await context.AuditLogs.FirstOrDefaultAsync();

            Assert.NotNull(saved);
            Assert.Equal(AuditAction.CREATE, saved!.Action);
            Assert.Equal("Joao123", saved.UserName);
            Assert.Equal("User", saved.EntityType);
        }

        [Fact]
        public async Task AddAsync_Should_Persist_To_Database()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new AppDbContext(options);

            var repository = new AuditLogRepository(context);

            var log = AuditLog.Create(
                        userId: Guid.NewGuid(),
                        userName: "Joao123",
                        action: AuditAction.CREATE,
                        entityType: "User",
                        entityId: Guid.NewGuid(),
                        oldValues: null,
                        newValues: "{\"name\":\"Joao\"}",
                        ipAddress: "127.0.0.1",
                        userAgent: "xUnit",
                        correlationId: Guid.NewGuid()
                    );

            await repository.AddAsync(log);

            Assert.Equal(1, await context.AuditLogs.CountAsync());
        }
    }
}