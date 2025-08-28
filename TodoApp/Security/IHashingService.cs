namespace TodoApp.Security;

public interface IHashingService
{
    // SHA2 digest in lowercase hex; algorithm examples: "SHA256", "SHA384", "SHA512"
    string Sha2(string input, string algorithm);

    // HMAC with provided key and algorithm; returns lowercase hex
    string Hmac(string input, byte[] key, string algorithm);

    // PBKDF2 encoded as: PBKDF2$ALGO$ITER$SALT_BASE64$HASH_BASE64
    string Pbkdf2Hash(string input, int iterations, string algorithm, int saltBytes);
    bool Pbkdf2Verify(string input, string encoded);

    // BCrypt hash and verify
    string BcryptHash(string input, int workFactor);
    bool BcryptVerify(string input, string hash);
}

