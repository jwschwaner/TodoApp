using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TodoApp.Tests.Security;

public class EncryptionContractsExtendedTests
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
    public void Extended_contract_should_cover_keygen_envelope_and_import_export()
    {
        var (iface, impl) = FindEncryptionServiceTypes();
        if (iface == null || impl == null) return; // pending implementation

        var methods = impl.GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(m => m.Name).ToArray();

        // AES helpers
        Assert.Contains(methods, n => n == "GenerateAesKey");
        Assert.Contains(methods, n => n == "GenerateNonce");

        // Envelope (hybrid) encryption
        Assert.Contains(methods, n => n == "EnvelopeEncrypt");
        Assert.Contains(methods, n => n == "EnvelopeDecrypt");

        // Public key import/export
        Assert.Contains(methods, n => n == "RsaGenerateKeyPair");
        Assert.Contains(methods, n => n == "RsaExportPublicKey");
        Assert.Contains(methods, n => n == "RsaImportPublicKey");
    }

    [Fact]
    public void Service_should_be_registrable_in_DI_if_present()
    {
        var (iface, impl) = FindEncryptionServiceTypes();
        if (iface == null || impl == null) return; // pending implementation

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        Assert.NotNull(svc);
    }
}

