using System.Security.Cryptography;
using System.Text;

namespace TodoApp.Security;

public class HashingService : IHashingService
{
    public string Sha2(string input, string algorithm)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = algorithm.ToUpperInvariant() switch
        {
            "SHA256" => SHA256.HashData(bytes),
            "SHA384" => SHA384.HashData(bytes),
            "SHA512" => SHA512.HashData(bytes),
            _ => throw new ArgumentException("Unsupported SHA2 algorithm.")
        };
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string Hmac(string input, byte[] key, string algorithm)
    {
        var data = Encoding.UTF8.GetBytes(input);
        byte[] mac = algorithm.ToUpperInvariant() switch
        {
            "SHA256" => new HMACSHA256(key).ComputeHash(data),
            "SHA384" => new HMACSHA384(key).ComputeHash(data),
            "SHA512" => new HMACSHA512(key).ComputeHash(data),
            _ => throw new ArgumentException("Unsupported HMAC algorithm.")
        };
        return Convert.ToHexString(mac).ToLowerInvariant();
    }

    public string Pbkdf2Hash(string input, int iterations, string algorithm, int saltBytes)
    {
        var salt = RandomNumberGenerator.GetBytes(saltBytes);
        var prf = algorithm.ToUpperInvariant() switch
        {
            "SHA1" => HashAlgorithmName.SHA1,
            "SHA256" => HashAlgorithmName.SHA256,
            "SHA384" => HashAlgorithmName.SHA384,
            "SHA512" => HashAlgorithmName.SHA512,
            _ => throw new ArgumentException("Unsupported PBKDF2 algorithm.")
        };
        var derived = Rfc2898DeriveBytes.Pbkdf2(input, salt, iterations, prf, 32);
        return $"PBKDF2${algorithm.ToUpperInvariant()}${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(derived)}";
    }

    public bool Pbkdf2Verify(string input, string encoded)
    {
        var parts = encoded.Split('$');
        if (parts.Length != 5 || parts[0] != "PBKDF2") return false;
        var algo = parts[1];
        if (!int.TryParse(parts[2], out var iter)) return false;
        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expected = Convert.FromBase64String(parts[4]);
        }
        catch { return false; }
        var prf = algo.ToUpperInvariant() switch
        {
            "SHA1" => HashAlgorithmName.SHA1,
            "SHA256" => HashAlgorithmName.SHA256,
            "SHA384" => HashAlgorithmName.SHA384,
            "SHA512" => HashAlgorithmName.SHA512,
            _ => throw new ArgumentException("Unsupported PBKDF2 algorithm.")
        };
        var actual = Rfc2898DeriveBytes.Pbkdf2(input, salt, iter, prf, expected.Length);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public string BcryptHash(string input, int workFactor)
    {
        return BCrypt.Net.BCrypt.HashPassword(input, workFactor);
    }

    public bool BcryptVerify(string input, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(input, hash);
    }
}

