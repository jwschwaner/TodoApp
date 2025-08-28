using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using TodoApp.TodoData;

namespace TodoApp.Tests.Infrastructure;

// Shared PostgreSQL container for integration tests.
// Starts once per test class (IClassFixture) and tears down when done.
public sealed class TestDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("todo_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await container.StartAsync();
        _container = container;
        ConnectionString = container.GetConnectionString();

        await EnsurePgcryptoAsync(ConnectionString);
        await ApplyMigrationsAsync(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public TodoDbContext CreateTodoDbContext()
    {
        var opts = new DbContextOptionsBuilder<TodoDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new TodoDbContext(opts);
    }

    private static async Task EnsurePgcryptoAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS pgcrypto;", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ApplyMigrationsAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var ctx = new TodoDbContext(options);
        await ctx.Database.MigrateAsync();
    }
}
