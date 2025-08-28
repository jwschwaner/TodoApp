namespace TodoApp.Security;

public interface IEncryptionService
{
    byte[] GenerateAesKey();
    byte[] GenerateNonce();

    byte[] AesGcmEncrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[]? aad = null);
    byte[] AesGcmDecrypt(byte[] key, byte[] nonce, byte[] ciphertextWithTag, byte[]? aad = null);

    (byte[] PrivateKeyPkcs8, byte[] PublicKeySpki) RsaGenerateKeyPair(int keySize = 2048);

    byte[] RsaEncrypt(byte[] publicKeySpki, byte[] plaintext, string hashAlg = "SHA256");
    byte[] RsaDecrypt(byte[] privateKeyPkcs8, byte[] ciphertext, string hashAlg = "SHA256");
    byte[] RsaSign(byte[] privateKeyPkcs8, byte[] data, string hashAlg = "SHA256");
    bool RsaVerify(byte[] publicKeySpki, byte[] data, byte[] signature, string hashAlg = "SHA256");

    byte[] RsaExportPublicKey(byte[] privateKeyPkcs8);
    byte[] RsaImportPublicKey(byte[] spki);

    byte[] EnvelopeEncrypt(byte[] publicKeySpki, byte[] plaintext, byte[]? aad = null);
    byte[] EnvelopeDecrypt(byte[] privateKeyPkcs8, byte[] envelope, byte[]? aad = null);
}

