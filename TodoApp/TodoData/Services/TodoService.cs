using Microsoft.EntityFrameworkCore;
using TodoApp.Security;

namespace TodoApp.TodoData.Services
{
    public class TodoService
    {
        private readonly TodoDbContext _context;
        private readonly IEncryptionService _enc;
        private readonly EncryptionKeyProvider _keys;

        public TodoService(TodoDbContext context, IEncryptionService enc, EncryptionKeyProvider keys)
        {
            _context = context;
            _enc = enc;
            _keys = keys;
        }

        public async Task<List<Todo>> GetTodosAsync(string cprNr, CancellationToken ct = default)
        {
            var list = await _context.Todos
                .AsNoTracking()
                .Where(t => t.CprNr == cprNr)
                .OrderBy(t => t.IsDone)
                .ThenBy(t => t.Id)
                .ToListAsync(ct);

            // Decrypt each item
            foreach (var t in list)
            {
                try
                {
                    var pt = _enc.EnvelopeDecrypt(_keys.PrivateKeyPkcs8, t.EncryptedItem);
                    t.Item = System.Text.Encoding.UTF8.GetString(pt);
                }
                catch
                {
                    t.Item = "<decryption failed>";
                }
            }
            return list;
        }

        public async Task<Todo> AddTodoAsync(string cprNr, string item, CancellationToken ct = default)
        {
            var plaintext = System.Text.Encoding.UTF8.GetBytes(item);
            var envelope = _enc.EnvelopeEncrypt(_keys.PublicKeySpki, plaintext);
            var todo = new Todo
            {
                CprNr = cprNr,
                EncryptedItem = envelope,
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
