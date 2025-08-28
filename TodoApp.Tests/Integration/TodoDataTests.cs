using Microsoft.EntityFrameworkCore;
using TodoApp.TodoData;
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
        await using var ctx = _fx.CreateTodoDbContext();

        var cpr = new Cpr { UserId = Guid.NewGuid().ToString("N"), CprNr = "1234567890" };
        ctx.Cprs.Add(cpr);
        await ctx.SaveChangesAsync();

        var todo = new Todo { CprNr = cpr.CprNr, Item = "Write tests", IsDone = false };
        ctx.Todos.Add(todo);
        await ctx.SaveChangesAsync();

        var fetched = await ctx.Todos.Include(t => t.Cpr)
            .Where(t => t.CprNr == cpr.CprNr)
            .FirstOrDefaultAsync();

        Assert.NotNull(fetched);
        Assert.Equal("Write tests", fetched!.Item);
        Assert.False(fetched.IsDone);
        Assert.NotNull(fetched.Cpr);
        Assert.Equal(cpr.CprNr, fetched.Cpr.CprNr);
    }
}
