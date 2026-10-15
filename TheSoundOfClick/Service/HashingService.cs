using System.Security.Cryptography;

namespace TheSoundOfClick;

public class HashingService
{
    public byte[] HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            600_000,
            HashAlgorithmName.SHA256,
            32
        );

        return salt.Concat(hash).ToArray();
    }

    public bool VerifyPassword(string password, byte[] storedPasswordHash)
    {
        if (storedPasswordHash == null || storedPasswordHash.Length != 48)
        {
            return false;
        }

        byte[] salt = storedPasswordHash[..16];
        byte[] originalHash = storedPasswordHash[16..];

        byte[] newHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            600_000,
            HashAlgorithmName.SHA256,
            32
        );

        return CryptographicOperations.FixedTimeEquals(originalHash, newHash);
    }
}