using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.TodoData;
using TodoApp.TodoData.Services;
using TodoApp.Tests.Infrastructure;
using Xunit;

namespace TodoApp.Tests.Integration;

public class CprUniquenessTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fx;
    public CprUniquenessTests(TestDatabaseFixture fx) => _fx = fx;

    [Fact]
    public async Task Same_CPR_for_different_users_should_be_rejected()
    {
        var sc = new ServiceCollection();
        sc.AddDbContext<TodoDbContext>(o => o.UseNpgsql(_fx.ConnectionString));
        sc.AddScoped<CprService>();
        sc.AddSingleton<TodoApp.Security.IHashingService>(new HashingStub());

        await using var sp = sc.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var cprSvc = scope.ServiceProvider.GetRequiredService<CprService>();

        var user1 = Guid.NewGuid().ToString("N");
        var user2 = Guid.NewGuid().ToString("N");
        var raw = "1111222233";

        Assert.True(await cprSvc.CreateCprAsync(user1, raw));
        Assert.False(await cprSvc.CreateCprAsync(user2, raw)); // must reject duplicate CPR across users

        // Different CPR should succeed
        Assert.True(await cprSvc.CreateCprAsync(user2, raw + "0"));
    }

    private sealed class HashingStub : TodoApp.Security.IHashingService
    {
        public string Sha2(string input, string algorithm) => $"STUB:SHA2:{algorithm}:{input}";
        public string Hmac(string input, byte[] key, string algorithm) => $"STUB:HMAC:{algorithm}:{input}";
        public string Pbkdf2Hash(string input, int iterations, string algorithm, int saltBytes) => $"STUB:PBKDF2:{algorithm}:{iterations}:{saltBytes}:{input}";
        public bool Pbkdf2Verify(string input, string encoded) => encoded.Contains($":{input}");
        public string BcryptHash(string input, int workFactor) => $"STUB:BCRYPT:{workFactor}:{input}";
        public bool BcryptVerify(string input, string encoded) => encoded.EndsWith($":{input}");
    }
}

