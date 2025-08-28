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
            // Preflight validation to avoid throwing and leaving tracked Added entities behind
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(cprNumber))
            {
                return false;
            }

            // If user already has a CPR, do not add another
            if (await _context.Cprs.AnyAsync(c => c.UserId == userId))
            {
                return false;
            }

            // Enforce unique CPR number across users
            if (await _context.Cprs.AnyAsync(c => c.CprNr == cprNumber))
            {
                return false;
            }

            var cpr = new Cpr
            {
                UserId = userId,
                CprNr = cprNumber
            };

            try
            {
                await _context.Cprs.AddAsync(cpr);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Detach the entity so a failed SaveChanges does not block future attempts
                try { _context.Entry(cpr).State = EntityState.Detached; } catch { }
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
