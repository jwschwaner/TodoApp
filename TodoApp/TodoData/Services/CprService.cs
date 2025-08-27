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
                    CprNr = cprNumber
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

        public async Task<bool> DeleteCprByUserIdAsync(string userId)
        {
            var cpr = await _context.Cprs.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cpr is null)
            {
                return false;
            }

            _context.Cprs.Remove(cpr);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
