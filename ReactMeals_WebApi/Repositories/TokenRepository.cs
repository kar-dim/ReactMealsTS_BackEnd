using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Repositories
{
    public class TokenRepository
    {
        private const string MANAGEMENT_API = "M_API";
        private readonly MainDbContext _context;
        public TokenRepository(MainDbContext context)
        {
            _context = context;
        }
        public async Task AddTokenAsync(Token token)
        {
            await _context.Tokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task<Token> GetManagementApiTokenAsync()
        {
            return await _context.Tokens.Where(x => x.TokenType.Equals(MANAGEMENT_API)).FirstOrDefaultAsync();
        }

        public async Task RemoveManagementApiTokenAsync()
        {
            Token token = await GetManagementApiTokenAsync();
            if (token != null)
                _context.Tokens.Remove(token);
        }
    }
}
