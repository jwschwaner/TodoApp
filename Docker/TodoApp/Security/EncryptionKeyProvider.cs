namespace TodoApp.Security;

public sealed class EncryptionKeyProvider
{
    public byte[] PrivateKeyPkcs8 { get; }
    public byte[] PublicKeySpki { get; }

    public EncryptionKeyProvider(IEncryptionService enc)
    {
        var privB64 = Environment.GetEnvironmentVariable("ENCRYPTION__RSA__PRIVATEKEY_PKCS8_BASE64");
        var pubB64  = Environment.GetEnvironmentVariable("ENCRYPTION__RSA__PUBLICKEY_SPKI_BASE64");
        if (!string.IsNullOrWhiteSpace(privB64) && !string.IsNullOrWhiteSpace(pubB64))
        {
            try
            {
                PrivateKeyPkcs8 = Convert.FromBase64String(privB64);
                PublicKeySpki   = Convert.FromBase64String(pubB64);
                return;
            }
            catch { }
        }

        var (priv, pub) = enc.RsaGenerateKeyPair();
        PrivateKeyPkcs8 = priv;
        PublicKeySpki   = pub;
    }
}

