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
        public async Task AddManagementApiTokenAsync(string tokenValue, DateTime expiryDate)
        {
            _context.Tokens.Add(new Token(tokenValue, MANAGEMENT_API, expiryDate));
            await _context.SaveChangesAsync();
        }

        public async Task<Token> GetManagementApiTokenAsync()
        {
            return await _context.Tokens.Where(token => token.TokenType.Equals(MANAGEMENT_API)).FirstOrDefaultAsync();
        }

        public async Task RemoveManagementApiTokenAsync()
        {
            Token token = await GetManagementApiTokenAsync();
            if (token != null)
                _context.Tokens.Remove(token);
        }
    }
}
