namespace AquaGas.Shared.Security;

using System.Security.Cryptography;
using System.Text;

public static class TokenHasher
{
    private static readonly Encoding Encoding = Encoding.UTF8;

    public static string Hash(string input)
    {
        var bytes = Encoding.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}