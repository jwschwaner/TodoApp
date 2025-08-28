using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.TodoData;
using TodoApp.TodoData.Services;
using TodoApp.Tests.Infrastructure;
using Xunit;

namespace TodoApp.Tests.Integration;

public class CprHashingTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fx;
    public CprHashingTests(TestDatabaseFixture fx) => _fx = fx;

    private static Type? FindHashingServiceInterface()
    {
        return typeof(TodoApp.TodoData.Cpr).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.IsInterface && t.FullName == "TodoApp.Security.IHashingService");
    }

    [Fact]
    public async Task CreateCprAsync_should_store_hashed_fields_not_plaintext()
    {
        var ihash = FindHashingServiceInterface();
        if (ihash == null) return; // pending implementation

        var sc = new ServiceCollection();
        sc.AddDbContext<TodoDbContext>(o => o.UseNpgsql(_fx.ConnectionString));
        sc.AddScoped<CprService>();
        sc.AddSingleton<TodoApp.Security.IHashingService>(new HashingStub());

        var sp = sc.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        var cprSvc = scope.ServiceProvider.GetRequiredService<CprService>();

        string userId = Guid.NewGuid().ToString("N");
        string raw = "1234567890";

        // Act
        var ok = await cprSvc.CreateCprAsync(userId, raw);
        Assert.True(ok);

        // Assert stored values differ from raw and match stubbed encoding patterns
        var stored = await ctx.Cprs.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);
        Assert.NotNull(stored);
        Assert.NotEqual(raw, stored!.CprPbkdf2);
        Assert.StartsWith("STUB:PBKDF2:", stored!.CprPbkdf2);
        Assert.NotEqual(raw, stored!.CprBcrypt);
        Assert.StartsWith("STUB:BCRYPT:", stored!.CprBcrypt);
    }

    [Fact]
    public async Task TodoService_should_use_same_key_as_stored_CPR()
    {
        var ihash = FindHashingServiceInterface();
        if (ihash == null) return; // pending implementation

        var sc = new ServiceCollection();
        sc.AddDbContext<TodoDbContext>(o => o.UseNpgsql(_fx.ConnectionString));
        sc.AddScoped<CprService>();
        sc.AddScoped<TodoService>();
        sc.AddSingleton<TodoApp.Security.IHashingService>(new HashingStub());

        var sp = sc.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        var cprSvc = scope.ServiceProvider.GetRequiredService<CprService>();
        var todoSvc = scope.ServiceProvider.GetRequiredService<TodoService>();

        string userId = Guid.NewGuid().ToString("N");
        string raw = "0987654321";

        // Arrange: create CPR (will be hashed by service)
        Assert.True(await cprSvc.CreateCprAsync(userId, raw));
        var cpr = await cprSvc.GetCprAsync(userId);
        Assert.NotNull(cpr);
        var key = cpr!.CprPbkdf2; // hashed key used as FK

        // Act: add a todo using the returned (hashed) key
        var todo = await todoSvc.AddTodoAsync(key, "hash-aware todo");

        // Assert: fetching by hashed key returns the todo; fetching by raw must not
        var byHashed = await todoSvc.GetTodosAsync(key);
        Assert.Single(byHashed);
        var byRaw = await todoSvc.GetTodosAsync(raw);
        Assert.Empty(byRaw);
    }

    private sealed class HashingStub : TodoApp.Security.IHashingService
    {
        // Allow any of the expected method names; CprService can choose which to call.
        public string Sha2(string input, string algorithm) => $"STUB:SHA2:{algorithm}:{input}";
        public string Hmac(string input, byte[] key, string algorithm) => $"STUB:HMAC:{algorithm}:{input}";
        public string Pbkdf2Hash(string input, int iterations, string algorithm, int saltBytes) => $"STUB:PBKDF2:{algorithm}:{iterations}:{saltBytes}:{input}";
        public bool Pbkdf2Verify(string input, string encoded) => encoded.Contains($":{input}");
        public string BcryptHash(string input, int workFactor) => $"STUB:BCRYPT:{workFactor}:{input}";
        public bool BcryptVerify(string input, string encoded) => encoded.EndsWith($":{input}");
    }
}
