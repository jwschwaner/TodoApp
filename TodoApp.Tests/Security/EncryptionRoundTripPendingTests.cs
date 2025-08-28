using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TodoApp.Tests.Security;

public class EncryptionRoundTripPendingTests
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
    public void Rsa_encrypt_decrypt_roundtrip_pending()
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
        if (gen == null || enc == null || dec == null) return;

        // Pending: invoke once shapes are defined (keypair/material types unknown yet)
    }

    [Fact]
    public void Envelope_encrypt_decrypt_roundtrip_pending()
    {
        var (iface, impl) = FindEncryptionServiceTypes();
        if (iface == null || impl == null) return;

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return;

        var envEnc = svc.GetType().GetMethod("EnvelopeEncrypt");
        var envDec = svc.GetType().GetMethod("EnvelopeDecrypt");
        if (envEnc == null || envDec == null) return;

        // Pending: round-trip test will be implemented once method signatures are finalized.
    }
}

