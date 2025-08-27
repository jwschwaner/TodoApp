using Microsoft.EntityFrameworkCore;

namespace TodoApp.TodoData.Services
{
    public class TodoService
    {
        private readonly TodoDbContext _context;

        public TodoService(TodoDbContext context)
        {
            _context = context;
        }

        public async Task<List<Todo>> GetTodosAsync(string cprNr, CancellationToken ct = default)
        {
            return await _context.Todos
                .AsNoTracking()
                .Where(t => t.CprNr == cprNr)
                .OrderBy(t => t.IsDone)
                .ThenBy(t => t.Item)
                .ToListAsync(ct);
        }

        public async Task<Todo> AddTodoAsync(string cprNr, string item, CancellationToken ct = default)
        {
            var todo = new Todo
            {
                CprNr = cprNr,
                Item = item,
                IsDone = false
            };

            await _context.Todos.AddAsync(todo, ct);
            await _context.SaveChangesAsync(ct);
            return todo;
        }

        public async Task<bool> SetDoneAsync(Guid id, bool isDone, CancellationToken ct = default)
        {
            var todo = await _context.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (todo is null) return false;
            todo.IsDone = isDone;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var todo = await _context.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (todo is null) return false;
            _context.Todos.Remove(todo);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}

