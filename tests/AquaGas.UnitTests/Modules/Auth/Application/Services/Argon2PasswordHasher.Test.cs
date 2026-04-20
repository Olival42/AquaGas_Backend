using Xunit;
using Microsoft.Extensions.Options;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Shared.Security;

public class Argon2PasswordHasherTests
{
    private Argon2PasswordHasher CreateHasher()
    {
        var options = Options.Create(new Argon2Options
        {
            TimeCost = 2,
            MemoryCost = 1024,
            Lanes = 2
        });

        return new Argon2PasswordHasher(options);
    }

    [Fact]
    public void Should_Generate_Hash()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash("123");

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void Hash_Should_Not_Be_Equal_To_Password()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash("123");

        Assert.NotEqual("123", hash);
    }

    [Fact]
    public void Should_Verify_Correct_Password()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash("123");

        var valid = hasher.Verify(hash, "123");

        Assert.True(valid);
    }

    [Fact]
    public void Should_Not_Verify_Wrong_Password()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash("123");

        var valid = hasher.Verify(hash, "wrong");

        Assert.False(valid);
    }

    [Fact]
    public void Should_Generate_Different_Hashes_For_Same_Password()
    {
        var hasher = CreateHasher();

        var hash1 = hasher.Hash("123");
        var hash2 = hasher.Hash("123");

        Assert.NotEqual(hash1, hash2);
    }
}