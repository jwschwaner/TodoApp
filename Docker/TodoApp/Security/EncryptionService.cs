using System.Security.Cryptography;

namespace TodoApp.Security;

public sealed class EncryptionService : IEncryptionService
{
    public byte[] GenerateAesKey() => RandomNumberGenerator.GetBytes(32);
    public byte[] GenerateNonce() => RandomNumberGenerator.GetBytes(12);

    public byte[] AesGcmEncrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[]? aad = null)
    {
        if (key is null || nonce is null || plaintext is null) throw new ArgumentNullException();
        if (key.Length != 16 && key.Length != 24 && key.Length != 32) throw new ArgumentException();
        if (nonce.Length < 12) throw new ArgumentException();
        var tagLen = 16;
        var tag = new byte[tagLen];
        var ciphertext = new byte[plaintext.Length];
        using var gcm = new AesGcm(key, tagLen);
        if (aad is { Length: > 0 }) gcm.Encrypt(nonce, plaintext, ciphertext, tag, aad);
        else gcm.Encrypt(nonce, plaintext, ciphertext, tag);
        var output = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, output, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, output, ciphertext.Length, tag.Length);
        return output;
    }

    public byte[] AesGcmDecrypt(byte[] key, byte[] nonce, byte[] ciphertextWithTag, byte[]? aad = null)
    {
        if (key is null || nonce is null || ciphertextWithTag is null) throw new ArgumentNullException();
        if (key.Length != 16 && key.Length != 24 && key.Length != 32) throw new ArgumentException();
        if (nonce.Length < 12) throw new ArgumentException();
        var tagLen = 16;
        if (ciphertextWithTag.Length < tagLen) throw new ArgumentException();
        var ciphertextLen = ciphertextWithTag.Length - tagLen;
        var ciphertext = new byte[ciphertextLen];
        var tag = new byte[tagLen];
        Buffer.BlockCopy(ciphertextWithTag, 0, ciphertext, 0, ciphertextLen);
        Buffer.BlockCopy(ciphertextWithTag, ciphertextLen, tag, 0, tagLen);
        var plaintext = new byte[ciphertextLen];
        using var gcm = new AesGcm(key, tagLen);
        if (aad is { Length: > 0 }) gcm.Decrypt(nonce, ciphertext, tag, plaintext, aad);
        else gcm.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    public (byte[] PrivateKeyPkcs8, byte[] PublicKeySpki) RsaGenerateKeyPair(int keySize = 2048)
    {
        if (keySize < 2048) throw new ArgumentException();
        using var rsa = RSA.Create(keySize);
        var priv = rsa.ExportPkcs8PrivateKey();
        var pub = rsa.ExportSubjectPublicKeyInfo();
        return (priv, pub);
    }

    public byte[] RsaEncrypt(byte[] publicKeySpki, byte[] plaintext, string hashAlg = "SHA256")
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKeySpki, out _);
        return rsa.Encrypt(plaintext, ResolveOaep(hashAlg));
    }

    public byte[] RsaDecrypt(byte[] privateKeyPkcs8, byte[] ciphertext, string hashAlg = "SHA256")
    {
        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);
        return rsa.Decrypt(ciphertext, ResolveOaep(hashAlg));
    }

    public byte[] RsaSign(byte[] privateKeyPkcs8, byte[] data, string hashAlg = "SHA256")
    {
        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);
        var (alg, _) = ResolveHash(hashAlg);
        return rsa.SignData(data, alg, RSASignaturePadding.Pss);
    }

    public bool RsaVerify(byte[] publicKeySpki, byte[] data, byte[] signature, string hashAlg = "SHA256")
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKeySpki, out _);
        var (alg, _) = ResolveHash(hashAlg);
        return rsa.VerifyData(data, signature, alg, RSASignaturePadding.Pss);
    }

    public byte[] RsaExportPublicKey(byte[] privateKeyPkcs8)
    {
        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);
        return rsa.ExportSubjectPublicKeyInfo();
    }

    public byte[] RsaImportPublicKey(byte[] spki)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(spki, out _);
        return spki;
    }

    public byte[] EnvelopeEncrypt(byte[] publicKeySpki, byte[] plaintext, byte[]? aad = null)
    {
        var aesKey = GenerateAesKey();
        var nonce = GenerateNonce();
        var ct = AesGcmEncrypt(aesKey, nonce, plaintext, aad);
        var encKey = RsaEncrypt(publicKeySpki, aesKey);
        var len = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(encKey.Length));
        var output = new byte[4 + encKey.Length + nonce.Length + ct.Length];
        Buffer.BlockCopy(len, 0, output, 0, 4);
        Buffer.BlockCopy(encKey, 0, output, 4, encKey.Length);
        Buffer.BlockCopy(nonce, 0, output, 4 + encKey.Length, nonce.Length);
        Buffer.BlockCopy(ct, 0, output, 4 + encKey.Length + nonce.Length, ct.Length);
        Array.Clear(aesKey, 0, aesKey.Length);
        return output;
    }

    public byte[] EnvelopeDecrypt(byte[] privateKeyPkcs8, byte[] envelope, byte[]? aad = null)
    {
        if (envelope.Length < 4 + 12 + 16) throw new ArgumentException();
        var lenBytes = new byte[4];
        Buffer.BlockCopy(envelope, 0, lenBytes, 0, 4);
        var encKeyLen = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenBytes, 0));
        if (encKeyLen <= 0 || envelope.Length < 4 + encKeyLen + 12 + 16) throw new ArgumentException();
        var encKey = new byte[encKeyLen];
        Buffer.BlockCopy(envelope, 4, encKey, 0, encKeyLen);
        var nonce = new byte[12];
        Buffer.BlockCopy(envelope, 4 + encKeyLen, nonce, 0, 12);
        var ctLen = envelope.Length - (4 + encKeyLen + 12);
        var ct = new byte[ctLen];
        Buffer.BlockCopy(envelope, 4 + encKeyLen + 12, ct, 0, ctLen);
        var aesKey = RsaDecrypt(privateKeyPkcs8, encKey);
        try { return AesGcmDecrypt(aesKey, nonce, ct, aad); }
        finally { Array.Clear(aesKey, 0, aesKey.Length); }
    }

    private static RSAEncryptionPadding ResolveOaep(string hash) => hash.ToUpperInvariant() switch
    {
        "SHA1" => RSAEncryptionPadding.OaepSHA1,
        "SHA256" => RSAEncryptionPadding.OaepSHA256,
        "SHA384" => RSAEncryptionPadding.OaepSHA384,
        "SHA512" => RSAEncryptionPadding.OaepSHA512,
        _ => throw new ArgumentException()
    };

    private static (HashAlgorithmName, string) ResolveHash(string hash) => hash.ToUpperInvariant() switch
    {
        "SHA1" => (HashAlgorithmName.SHA1, "SHA1"),
        "SHA256" => (HashAlgorithmName.SHA256, "SHA256"),
        "SHA384" => (HashAlgorithmName.SHA384, "SHA384"),
        "SHA512" => (HashAlgorithmName.SHA512, "SHA512"),
        _ => throw new ArgumentException()
    };
}

