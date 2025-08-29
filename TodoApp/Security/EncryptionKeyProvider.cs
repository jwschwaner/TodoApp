using Microsoft.Extensions.DependencyInjection;

namespace TodoApp.Security;

public sealed class EncryptionKeyProvider
{
    public byte[] PrivateKeyPkcs8 { get; }
    public byte[] PublicKeySpki { get; }

    public EncryptionKeyProvider(IEncryptionService enc, IServiceProvider services)
    {
        // Try to get IConfiguration if available (app runtime); tests may not register it
        var config = services.GetService<IConfiguration>();
        if (config is not null)
        {
            var privB64Cfg = config["ENCRYPTION__RSA__PRIVATEKEY_PKCS8_BASE64"]; 
            var pubB64Cfg  = config["ENCRYPTION__RSA__PUBLICKEY_SPKI_BASE64"];  
            if (!string.IsNullOrWhiteSpace(privB64Cfg) && !string.IsNullOrWhiteSpace(pubB64Cfg))
            {
                try
                {
                    PrivateKeyPkcs8 = Convert.FromBase64String(privB64Cfg);
                    PublicKeySpki   = Convert.FromBase64String(pubB64Cfg);
                    return;
                }
                catch { /* fallthrough to env/generate */ }
            }
        }

        // Fallback to environment variables
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
            catch { /* fallthrough to generate */ }
        }

        // Generate a new pair (development/test fallback)
        var (priv, pub) = enc.RsaGenerateKeyPair();
        PrivateKeyPkcs8 = priv;
        PublicKeySpki   = pub;
    }
}
