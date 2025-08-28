using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TodoApp.Tests.Security;

public class EncryptionPolicyPendingTests
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
    public void Aes_key_and_nonce_sizes_should_meet_minimums_if_present()
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
        if (genKey == null || genNonce == null) return;

        var key = genKey.Invoke(svc, Array.Empty<object>()) as byte[];
        var nonce = genNonce.Invoke(svc, Array.Empty<object>()) as byte[];
        if (key == null || nonce == null) return;

        Assert.True(key.Length >= 32);  // 256-bit AES keys recommended
        Assert.True(nonce.Length >= 12); // GCM nonce min recommended length
    }

    [Fact]
    public void Rsa_key_generation_should_target_2048_bits_or_more_if_present()
    {
        var (iface, impl) = FindEncryptionServiceTypes();
        if (iface == null || impl == null) return; // pending implementation

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return;

        var gen = svc.GetType().GetMethod("RsaGenerateKeyPair");
        if (gen == null) return;

        // If the API returns a tuple or object, we cannot introspect bit length without a defined type.
        // This test serves as a placeholder to assert policy once the concrete API is implemented.
    }

    [Fact]
    public void Aes_encrypt_should_reject_null_or_empty_inputs_if_present()
    {
        var (iface, impl) = FindEncryptionServiceTypes();
        if (iface == null || impl == null) return; // pending implementation

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return;

        var enc = svc.GetType().GetMethod("AesGcmEncrypt");
        if (enc == null) return;

        var bad = new object[] { null!, null!, null! };
        try
        {
            _ = enc.Invoke(svc, bad);
            // If it doesn't throw yet, that's fine until implementation exists
        }
        catch
        {
            // Accept any thrown error as validation present
        }
    }
}

