namespace AquaGas.Api.Modules.Auth.Application.Services;

using System.Text;
using AquaGas.Api.Shared.Security;
using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Options;

public class Argon2PasswordHasher : IPasswordHasher
{
    private readonly Argon2Options _options;
    private static readonly Encoding Encoding = Encoding.UTF8;

    public Argon2PasswordHasher(IOptions<Argon2Options> options)
    {
        _options = options.Value;
    }

    public string Hash(string password)
    {
        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = _options.TimeCost,
            MemoryCost = _options.MemoryCost,
            Lanes = _options.Lanes,
            Password = Encoding.GetBytes(password),
            Salt = Guid.NewGuid().ToByteArray()
        };

        return Argon2.Hash(config);
    }

    public bool Verify(string hash, string password)
        => Argon2.Verify(hash, password);
}