using Moq;
using Xunit;
using AquaGas.Shared.Infrastructure.TokenBlacklist;
using AquaGas.Shared.Infrastructure.Cache;

namespace AquaGas.Tests.Shared.Infrastructure.TokenBlacklist
{
    public class TokenBlacklistServiceTests
    {
        private readonly Mock<IRedisService> _redisMock;
        private readonly TokenBlacklistService _service;

        public TokenBlacklistServiceTests()
        {
            _redisMock = new Mock<IRedisService>();
            _service = new TokenBlacklistService(_redisMock.Object);
        }

        [Fact]
        public async Task AddAsync_Should_Call_Redis_With_Ttl()
        {
            var token = "token";
            var expires = DateTime.UtcNow.AddMinutes(10);

            await _service.AddAsync(token, expires);

            _redisMock.Verify(x => x.SetAsync(
                $"blacklist:access:{token}",
                "revoked",
                It.IsAny<TimeSpan>()),
                Times.Once);
        }

        [Fact]
        public async Task AddAsync_Should_Not_Call_Redis_When_Expired()
        {
            var token = "token";
            var expires = DateTime.UtcNow.AddMinutes(-1);

            await _service.AddAsync(token, expires);

            _redisMock.Verify(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()),
                Times.Never);
        }

        [Fact]
        public async Task IsBlacklistedAsync_Should_Return_True_When_Exists()
        {
            var token = "token";

            _redisMock
                .Setup(x => x.GetAsync($"blacklist:access:{token}"))
                .ReturnsAsync("revoked");

            var result = await _service.IsBlacklistedAsync(token);

            Assert.True(result);
        }

        [Fact]
        public async Task IsBlacklistedAsync_Should_Return_False_When_Not_Exists()
        {
            var token = "token";

            _redisMock
                .Setup(x => x.GetAsync($"blacklist:access:{token}"))
                .ReturnsAsync((string?)null);

            var result = await _service.IsBlacklistedAsync(token);

            Assert.False(result);
        }
    }
}