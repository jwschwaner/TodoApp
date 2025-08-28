using Microsoft.EntityFrameworkCore;
using TodoApp.Security;

namespace TodoApp.TodoData.Services
{
    public class CprService
    {
        private readonly TodoDbContext _context;
        private readonly IHashingService _hashing;

        public CprService(TodoDbContext context, IHashingService hashing)
        {
            _context = context;
            _hashing = hashing;
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
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(cprNumber))
            {
                return false;
            }

            if (await _context.Cprs.AnyAsync(c => c.UserId == userId))
            {
                return false;
            }

            // Hash with PBKDF2 and store directly in CprNr
            var pbkdf2 = _hashing.Pbkdf2Hash(cprNumber, ResolvePbkdf2Iterations(), "SHA256", ResolvePbkdf2SaltBytes());

            var cpr = new Cpr
            {
                UserId = userId,
                CprNr = pbkdf2
            };

            try
            {
                await _context.Cprs.AddAsync(cpr);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
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

        private static int ResolvePbkdf2Iterations()
        {
            var s = Environment.GetEnvironmentVariable("HASHING__CPR__PBKDF2__ITERATIONS");
            if (int.TryParse(s, out var value) && value > 0) return value;
            return 100_000;
        }

        private static int ResolvePbkdf2SaltBytes()
        {
            var s = Environment.GetEnvironmentVariable("HASHING__CPR__PBKDF2__SALTBYTES");
            if (int.TryParse(s, out var value) && value > 0) return value;
            return 16;
        }
    }
}
