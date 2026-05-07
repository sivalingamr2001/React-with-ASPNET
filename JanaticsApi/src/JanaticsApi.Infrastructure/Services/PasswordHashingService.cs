using JanaticsApi.Domain.Services;
using System.Security.Cryptography;

namespace JanaticsApi.Infrastructure.Services;

public sealed class PasswordHashingService : IPasswordHashingService
{
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA512,
            64);

        return Convert.ToBase64String(salt.Concat(hash).ToArray());
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        var data = Convert.FromBase64String(passwordHash);
        var salt = data[..16];
        var expectedHash = data[16..];

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA512,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
