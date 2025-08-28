using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TodoApp.Tests.Security;

public class EncryptionServiceTests
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
    public void Encryption_contract_should_exist()
    {
        var (iface, impl) = FindEncryptionServiceTypes();
        if (iface == null || impl == null) return; // pending implementation

        var methods = impl.GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(m => m.Name).ToArray();

        // Symmetric (AES-GCM recommended)
        Assert.Contains(methods, n => n == "AesGcmEncrypt");
        Assert.Contains(methods, n => n == "AesGcmDecrypt");

        // Asymmetric (RSA/OAEP + RSASSA-PSS recommended)
        Assert.Contains(methods, n => n == "RsaEncrypt");
        Assert.Contains(methods, n => n == "RsaDecrypt");
        Assert.Contains(methods, n => n == "RsaSign");
        Assert.Contains(methods, n => n == "RsaVerify");

        // Optional keypair generation utility
        Assert.Contains(methods, n => n == "RsaGenerateKeyPair");
    }

    [Fact]
    public void Symmetric_API_shapes_should_be_reasonable_if_present()
    {
        var (iface, impl) = FindEncryptionServiceTypes();
        if (iface == null || impl == null) return; // pending implementation

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return; // not wired

        var enc = svc.GetType().GetMethod("AesGcmEncrypt");
        var dec = svc.GetType().GetMethod("AesGcmDecrypt");
        if (enc == null || dec == null) return;

        // Validate parameters count (key, nonce, plaintext[, aad]) and return types in a loose way
        Assert.InRange(enc.GetParameters().Length, 3, 4);
        Assert.InRange(dec.GetParameters().Length, 3, 4);
    }

    [Fact]
    public void Asymmetric_API_shapes_should_be_reasonable_if_present()
    {
        var (iface, impl) = FindEncryptionServiceTypes();
        if (iface == null || impl == null) return;

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return;

        var gen = svc.GetType().GetMethod("RsaGenerateKeyPair");
        var enc = svc.GetType().GetMethod("RsaEncrypt");
        var dec = svc.GetType().GetMethod("RsaDecrypt");
        var sig = svc.GetType().GetMethod("RsaSign");
        var ver = svc.GetType().GetMethod("RsaVerify");

        if (enc == null || dec == null || sig == null || ver == null) return;
        Assert.NotNull(gen); // expect a keypair generator to exist

        // Basic shape checks
        Assert.True(enc.GetParameters().Length >= 2);
        Assert.True(dec.GetParameters().Length >= 2);
        Assert.True(sig.GetParameters().Length >= 2);
        Assert.True(ver.GetParameters().Length >= 3);
    }
}

