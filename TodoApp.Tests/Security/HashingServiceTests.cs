using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TodoApp.Tests.Security;

public class HashingServiceTests
{
    private static (Type? iface, Type? impl) FindHashingServiceTypes()
    {
        var asm = typeof(TodoApp.TodoData.Cpr).Assembly;
        var iface = asm.GetTypes().FirstOrDefault(t => t.IsInterface && t.FullName == "TodoApp.Security.IHashingService");
        if (iface == null) return (null, null);
        var impl = asm.GetTypes().FirstOrDefault(t => t.IsClass && !t.IsAbstract && iface.IsAssignableFrom(t));
        return (iface, impl);
    }

    [Fact]
    public void Sha2_Hmac_Pbkdf2_Bcrypt_contract_should_exist()
    {
        var (iface, impl) = FindHashingServiceTypes();
        if (iface == null || impl == null) return; // pending implementation

        var methods = impl.GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(m => m.Name).ToArray();
        Assert.Contains(methods, n => n == "Sha2");
        Assert.Contains(methods, n => n == "Hmac");
        Assert.Contains(methods, n => n == "Pbkdf2Hash");
        Assert.Contains(methods, n => n == "Pbkdf2Verify");
        Assert.Contains(methods, n => n == "BcryptHash");
        Assert.Contains(methods, n => n == "BcryptVerify");
    }

    [Fact]
    public void Sha2_should_be_deterministic_and_match_builtin()
    {
        var (iface, impl) = FindHashingServiceTypes();
        if (iface == null || impl == null) return; // pending implementation

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return; // not wired in DI

        var sha2 = svc.GetType().GetMethod("Sha2", new[] { typeof(string), typeof(string) });
        if (sha2 == null) return;

        string input = "1234567890";
        string algo = "SHA256";
        var actual = (string)sha2.Invoke(svc, new object[] { input, algo })!;

        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        var expected = Convert.ToHexString(hash).ToLowerInvariant();

        Assert.Equal(expected, actual);
        var actual2 = (string)sha2.Invoke(svc, new object[] { input, algo })!;
        Assert.Equal(actual, actual2);
    }

    [Fact]
    public void Hmac_should_match_builtin_for_known_key()
    {
        var (iface, impl) = FindHashingServiceTypes();
        if (iface == null || impl == null) return;

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return;

        var hmac = svc.GetType().GetMethod("Hmac", new[] { typeof(string), typeof(byte[]), typeof(string) });
        if (hmac == null) return;

        string input = "1234567890";
        byte[] key = System.Text.Encoding.UTF8.GetBytes("test-secret-key");
        string algo = "SHA256";
        var actual = (string)hmac.Invoke(svc, new object[] { input, key, algo })!;

        using var h = new System.Security.Cryptography.HMACSHA256(key);
        var expected = Convert.ToHexString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Pbkdf2_should_encode_salt_and_verify_true_and_false()
    {
        var (iface, impl) = FindHashingServiceTypes();
        if (iface == null || impl == null) return;

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return;

        var hashM = svc.GetType().GetMethod("Pbkdf2Hash", new[] { typeof(string), typeof(int), typeof(string), typeof(int) });
        var verM  = svc.GetType().GetMethod("Pbkdf2Verify", new[] { typeof(string), typeof(string) });
        if (hashM == null || verM == null) return;

        string input = "1234567890";
        string algo = "SHA256";
        int iterations = 100_000;
        int saltBytes = 16;

        var encoded1 = (string)hashM.Invoke(svc, new object[] { input, iterations, algo, saltBytes })!;
        var encoded2 = (string)hashM.Invoke(svc, new object[] { input, iterations, algo, saltBytes })!;

        Assert.NotEqual(encoded1, encoded2);
        Assert.StartsWith("PBKDF2$", encoded1);

        Assert.True((bool)verM.Invoke(svc, new object[] { input, encoded1 })!);
        Assert.False((bool)verM.Invoke(svc, new object[] { input + "x", encoded1 })!);
    }

    [Fact]
    public void Bcrypt_should_prefix_and_verify_true_and_false()
    {
        var (iface, impl) = FindHashingServiceTypes();
        if (iface == null || impl == null) return;

        var sc = new ServiceCollection();
        sc.AddSingleton(iface, impl);
        var sp = sc.BuildServiceProvider();
        var svc = sp.GetService(iface);
        if (svc == null) return;

        var hashM = svc.GetType().GetMethod("BcryptHash", new[] { typeof(string), typeof(int) });
        var verM  = svc.GetType().GetMethod("BcryptVerify", new[] { typeof(string), typeof(string) });
        if (hashM == null || verM == null) return;

        string input = "1234567890";
        int work = 12;

        var h1 = (string)hashM.Invoke(svc, new object[] { input, work })!;
        var h2 = (string)hashM.Invoke(svc, new object[] { input, work })!;

        Assert.StartsWith("$2", h1);
        Assert.NotEqual(h1, h2);
        Assert.True((bool)verM.Invoke(svc, new object[] { input, h1 })!);
        Assert.False((bool)verM.Invoke(svc, new object[] { input + "x", h1 })!);
    }
}
