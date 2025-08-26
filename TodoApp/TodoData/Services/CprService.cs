using Microsoft.EntityFrameworkCore;

namespace TodoApp.TodoData.Services
{
    public class CprService
    {
        private readonly TodoDbContext _context;

        public CprService(TodoDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasCprAsync(string userId)
        {
            return await _context.Cprs.AnyAsync(c => c.UserId == userId);
        }

        public async Task<Cpr?> GetCprAsync(string userId)
        {
            return await _context.Cprs.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<bool> CreateCprAsync(string userId, string cprNumber)
        {
            try
            {
                var cpr = new Cpr
                {
                    UserId = userId,
                    CprNr = cprNumber,
                    Todos = new List<Todo>()
                };

                await _context.Cprs.AddAsync(cpr);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
