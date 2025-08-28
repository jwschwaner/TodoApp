using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.TodoData;
using TodoApp.TodoData.Services;
using TodoApp.Tests.Infrastructure;

namespace TodoApp.Tests.Integration;

public class TodoDataTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fx;
    public TodoDataTests(TestDatabaseFixture fx) => _fx = fx;

    [Fact]
    public async Task Database_is_up_and_migrated()
    {
        await using var ctx = _fx.CreateTodoDbContext();
        Assert.True(await ctx.Database.CanConnectAsync());

        // Tables exist (simple smoke check: try a count query on both)
        _ = await ctx.Cprs.CountAsync();
        _ = await ctx.Todos.CountAsync();
    }

    [Fact]
    public async Task Can_create_Cpr_and_Todo_and_query_back()
    {
        // Build a minimal DI graph to use CprService and TodoService
        var sc = new ServiceCollection();
        sc.AddDbContext<TodoDbContext>(o => o.UseNpgsql(_fx.ConnectionString));
        sc.AddScoped<CprService>();
        sc.AddScoped<TodoService>();
        // Hashing + Encryption services
        sc.AddSingleton<TodoApp.Security.IHashingService>(new HashingStub());
        sc.AddSingleton<TodoApp.Security.IEncryptionService, TodoApp.Security.EncryptionService>();
        sc.AddSingleton<TodoApp.Security.EncryptionKeyProvider>();
        await using var sp = sc.BuildServiceProvider();
        using var scope = sp.CreateScope();

        var ctx = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        var cprSvc = scope.ServiceProvider.GetRequiredService<CprService>();
        var todoSvc = scope.ServiceProvider.GetRequiredService<TodoService>();

        var userId = Guid.NewGuid().ToString("N");
        var rawCpr = "1234567890";
        Assert.True(await cprSvc.CreateCprAsync(userId, rawCpr));
        var stored = await cprSvc.GetCprAsync(userId);
        Assert.NotNull(stored);
        var cprKey = stored!.CprPbkdf2; // hashed key used as FK

        var todo = await todoSvc.AddTodoAsync(cprKey, "Write tests");

        // Use service to fetch decrypted items
        var list = await todoSvc.GetTodosAsync(cprKey);
        Assert.Single(list);
        var fetched = list.First();

        Assert.NotNull(fetched);
        Assert.Equal("Write tests", fetched!.Item);
        Assert.False(fetched.IsDone);

        // Also confirm relation and encrypted payload exist in DB
        var dbRow = await ctx.Todos.Include(t => t.Cpr)
            .Where(t => t.Id == fetched.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(dbRow);
        Assert.NotNull(dbRow!.EncryptedItem);
        Assert.True(dbRow.EncryptedItem.Length > 0);
        Assert.NotNull(dbRow.Cpr);
        Assert.Equal(cprKey, dbRow.Cpr.CprPbkdf2);
    }

    private sealed class HashingStub : TodoApp.Security.IHashingService
    {
        public string Sha2(string input, string algorithm) => $"STUB:SHA2:{algorithm}:{input}";
        public string Hmac(string input, byte[] key, string algorithm) => $"STUB:HMAC:{algorithm}:{input}";
        public string Pbkdf2Hash(string input, int iterations, string algorithm, int saltBytes) => $"PBKDF2:{algorithm}:{iterations}:{saltBytes}:{input}";
        public bool Pbkdf2Verify(string input, string encoded) => encoded.Contains($":{input}");
        public string BcryptHash(string input, int workFactor) => $"$2stub$wf{workFactor}${input}";
        public bool BcryptVerify(string input, string hash) => hash.EndsWith($"${input}");
    }
}
