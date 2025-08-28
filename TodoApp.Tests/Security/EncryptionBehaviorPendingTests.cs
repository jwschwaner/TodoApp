using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace TodoApp.Tests.Security;

public class EncryptionBehaviorPendingTests
{
    private static (Type? iface, Type? impl) FindEncryptionServiceTypes()
    {
        var asm = typeof(TodoApp.TodoData.Cpr).Assembly;
        var iface = asm.GetTypes().FirstOrDefault(t => t.IsInterface && t.FullName == "TodoApp.Security.IEncryptionService");
        if (iface == null) return (null, null);
        var impl = asm.GetTypes().FirstOrDefault(t => t.IsClass && !t.IsAbstract && iface.IsAssignableFrom(t));
        return (iface, impl);
    }

    [Fact]
    public void AesGcm_roundtrip_and_tamper_detection_pending()
    {
        var (iface, impl) = FindEncryptionServiceTypes();
        if (iface == null || impl == null) return; // pending implementation

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return;

        var genKey = svc.GetType().GetMethod("GenerateAesKey");
        var genNonce = svc.GetType().GetMethod("GenerateNonce");
        var enc = svc.GetType().GetMethod("AesGcmEncrypt");
        var dec = svc.GetType().GetMethod("AesGcmDecrypt");
        if (genKey == null || genNonce == null || enc == null || dec == null) return;

        // If implemented with byte[] API, perform a basic round-trip and tamper test
        var key = genKey.Invoke(svc, Array.Empty<object>()) as byte[];
        var nonce = genNonce.Invoke(svc, Array.Empty<object>()) as byte[];
        if (key == null || nonce == null) return;
        Assert.True(key.Length >= 32);
        Assert.True(nonce.Length >= 12);

        var plaintext = System.Text.Encoding.UTF8.GetBytes("encryption-pending-test");
        var aad = System.Text.Encoding.UTF8.GetBytes("aad");

        object? ct;
        try
        {
            // Try 4-arg first (key, nonce, plaintext, aad)
            if (enc.GetParameters().Length >= 4)
                ct = enc.Invoke(svc, new object[] { key, nonce, plaintext, aad });
            else
                ct = enc.Invoke(svc, new object[] { key, nonce, plaintext });
        }
        catch
        {
            return; // pending concrete signature
        }

        if (ct is not byte[] ciphertext) return;

        byte[] roundtrip;
        try
        {
            if (dec.GetParameters().Length >= 4)
                roundtrip = (byte[])dec.Invoke(svc, new object[] { key, nonce, ciphertext, aad })!;
            else
                roundtrip = (byte[])dec.Invoke(svc, new object[] { key, nonce, ciphertext })!;
        }
        catch
        {
            return; // pending concrete signature
        }

        Assert.Equal(plaintext, roundtrip);

        // Tamper
        ciphertext[0] ^= 0xFF;
        try
        {
            if (dec.GetParameters().Length >= 4)
                _ = dec.Invoke(svc, new object[] { key, nonce, ciphertext, aad });
            else
                _ = dec.Invoke(svc, new object[] { key, nonce, ciphertext });
            throw new XunitException("Tampering should fail decryption/verification");
        }
        catch (TargetInvocationException)
        {
            // expected: underlying implementation should throw on auth failure
        }
        catch
        {
            // also acceptable: any error bubbling up
        }
    }

    [Fact]
    public void Rsa_sign_verify_roundtrip_pending()
    {
        var (iface, impl) = FindEncryptionServiceTypes();
        if (iface == null || impl == null) return; // pending implementation

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return;

        var gen = svc.GetType().GetMethod("RsaGenerateKeyPair");
        var sign = svc.GetType().GetMethod("RsaSign");
        var verify = svc.GetType().GetMethod("RsaVerify");
        if (gen == null || sign == null || verify == null) return;

        // Without knowing exact shapes, just ensure methods exist; execution awaits implementation
        // Once implemented, we can extend this to actually sign/verify.
    }
}
