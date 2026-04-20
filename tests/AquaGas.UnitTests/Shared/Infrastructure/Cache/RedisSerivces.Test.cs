using Xunit;
using Moq;
using StackExchange.Redis;
using AquaGas.Api.Shared.Infrastructure.Cache;

public class RedisServiceTests
{
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<IConnectionMultiplexer> _mockConnection;
    private readonly RedisService _redisService;

    public RedisServiceTests()
    {
        _mockDatabase = new Mock<IDatabase>();

        _mockConnection = new Mock<IConnectionMultiplexer>();
        _mockConnection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _redisService = new RedisService(_mockConnection.Object);
    }

    [Fact]
    public async Task SetAsync_Should_Call_StringSetAsync_With_Valid_Parameters()
    {
        _mockDatabase.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>()
        )).ReturnsAsync(true);

        await _redisService.SetAsync("key", "value", TimeSpan.FromMinutes(30));

        _mockDatabase.Verify(db => db.StringSetAsync(
            "key",
            "value",
            It.IsAny<Expiration>()
        ), Times.Once);
    }

    [Fact]
    public async Task SetAsync_Should_Handle_Different_Expirations()
    {
        _mockDatabase.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>()
        )).ReturnsAsync(true);

        await _redisService.SetAsync("key", "value", TimeSpan.Zero);
        await _redisService.SetAsync("key", "value", TimeSpan.FromMinutes(100));

        _mockDatabase.Verify(db => db.StringSetAsync(
            "key",
            "value",
            It.IsAny<Expiration>()
        ), Times.Exactly(2));
    }

    [Fact]
    public async Task SetAsync_Should_Throw_When_DatabaseFails()
    {
        _mockDatabase.Setup(db => db.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>()
        )).ThrowsAsync(new Exception("Redis falhou"));

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _redisService.SetAsync("key", "value", TimeSpan.FromMinutes(1))
        );

        Assert.Equal("Redis falhou", ex.Message);

        _mockDatabase.Verify(db => db.StringSetAsync(
            "key",
            "value",
            It.IsAny<Expiration>()
        ), Times.Once);
    }

    [Fact]
    public async Task Set_Async_Should_Handle_Empty_And_Null_Values()
    {
        _mockDatabase.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>()
        )).ReturnsAsync(true);

        await _redisService.SetAsync("key", "", TimeSpan.Zero);
        await _redisService.SetAsync("key", null!, TimeSpan.FromMinutes(100));

        _mockDatabase.Verify(db => db.StringSetAsync(
            "key",
            "",
            It.IsAny<Expiration>()),
            Times.Once);

        _mockDatabase.Verify(db => db.StringSetAsync(
            "key",
            RedisValue.Null,
            It.IsAny<Expiration>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_Should_Handle_Multiple_Different_Keys()
    {
        _mockDatabase.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>()
        )).ReturnsAsync(true);

        var keysAndValues = new List<(string key, string value)>
        {
            ("key1", "val1"),
            ("key2", "val2")
        };

        foreach (var kv in keysAndValues)
        {
            await _redisService.SetAsync(kv.key, kv.value, TimeSpan.FromMinutes(1));
        }

        foreach (var kv in keysAndValues)
        {
            _mockDatabase.Verify(db => db.StringSetAsync(
                kv.key,
                kv.value,
                It.IsAny<Expiration>()
            ), Times.Once);
        }
    }

    [Fact]
    public async Task SetAsync_Should_Handle_Large_Number_Of_Calls()
    {
        _mockDatabase.Setup(db => db.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>()
        )).ReturnsAsync(true);

        var keys = Enumerable.Range(1, 1000).Select(i => $"key{i}").ToList();

        foreach (var key in keys)
        {
            await _redisService.SetAsync(key, $"value-{key}", TimeSpan.FromMinutes(5));
        }

        _mockDatabase.Verify(db => db.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>()
        ), Times.Exactly(1000));
    }

    [Fact]
    public async Task GetAsync_Should_Return_Value_When_Key_Exists()
    {
        var value = "value";

        _mockDatabase.Setup(d => d.StringGetAsync(
           "key"
        )).ReturnsAsync(value);

        var result = await _redisService.GetAsync("key");

        Assert.Equal(value, result);

        _mockDatabase.Verify(db => db.StringGetAsync(
            "key"
        ), Times.Once);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Null_When_Key_Does_Not_Exist()
    {
        var value = RedisValue.Null;

        _mockDatabase.Setup(d => d.StringGetAsync(
           "key"
        )).ReturnsAsync(value);

        var result = await _redisService.GetAsync("key");

        Assert.Null(result);

        _mockDatabase.Verify(db => db.StringGetAsync(
            "key"
        ), Times.Once);
    }

    [Fact]
    public async Task GetAsync_Should_Throw_Exception_When_Database_Fails()
    {
        var exception = new Exception("Redis falhou");

        _mockDatabase.Setup(d => d.StringGetAsync(
           "key"
        )).ThrowsAsync(exception);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _redisService.GetAsync("key"));

        Assert.Equal(exception.Message, ex.Message);

        _mockDatabase.Verify(db => db.StringGetAsync(
            "key"
        ), Times.Once);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Empty_String_When_Value_Is_Empty()
    {
        var value = "";

        _mockDatabase.Setup(d => d.StringGetAsync(
           "key"
        )).ReturnsAsync(value);

        var result = await _redisService.GetAsync("key");

        Assert.Equal(value, result);

        _mockDatabase.Verify(db => db.StringGetAsync(
            "key"
        ), Times.Once);
    }
}