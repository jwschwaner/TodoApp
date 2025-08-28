namespace TodoApp.Security;

public interface IEncryptionService
{
    // AES-GCM helpers
    byte[] GenerateAesKey();
    byte[] GenerateNonce();

    // AES-GCM (single shape with optional AAD)
    byte[] AesGcmEncrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[]? aad = null);
    byte[] AesGcmDecrypt(byte[] key, byte[] nonce, byte[] ciphertextWithTag, byte[]? aad = null);

    // RSA keypair
    (byte[] PrivateKeyPkcs8, byte[] PublicKeySpki) RsaGenerateKeyPair(int keySize = 2048);

    // RSA crypto (OAEP for encrypt/decrypt, PSS for sign/verify)
    byte[] RsaEncrypt(byte[] publicKeySpki, byte[] plaintext, string hashAlg = "SHA256");
    byte[] RsaDecrypt(byte[] privateKeyPkcs8, byte[] ciphertext, string hashAlg = "SHA256");
    byte[] RsaSign(byte[] privateKeyPkcs8, byte[] data, string hashAlg = "SHA256");
    bool RsaVerify(byte[] publicKeySpki, byte[] data, byte[] signature, string hashAlg = "SHA256");

    // Public key import/export utilities
    byte[] RsaExportPublicKey(byte[] privateKeyPkcs8);
    byte[] RsaImportPublicKey(byte[] spki);

    // Envelope (hybrid) encryption
    byte[] EnvelopeEncrypt(byte[] publicKeySpki, byte[] plaintext, byte[]? aad = null);
    byte[] EnvelopeDecrypt(byte[] privateKeyPkcs8, byte[] envelope, byte[]? aad = null);
}
