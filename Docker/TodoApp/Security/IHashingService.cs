namespace TodoApp.Security;

public interface IHashingService
{
    string Sha2(string input, string algorithm);
    string Hmac(string input, byte[] key, string algorithm);
    string Pbkdf2Hash(string input, int iterations, string algorithm, int saltBytes);
    bool Pbkdf2Verify(string input, string encoded);
    string BcryptHash(string input, int workFactor);
    bool BcryptVerify(string input, string hash);
}

