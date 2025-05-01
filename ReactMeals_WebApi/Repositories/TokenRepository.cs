using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Repositories;

public class TokenRepository(MainDbContext context)
{
    public async Task AddManagementApiTokenAsync(string tokenValue, DateTime expiryDate)
    {
        context.Tokens.Add(new Token(tokenValue, TokenType.MANAGEMENT_API, expiryDate));
        await context.SaveChangesAsync();
    }

    public async Task<Token> GetManagementApiTokenAsync()
    {
        return await context.Tokens.Where(token => token.TokenType.Equals(TokenType.MANAGEMENT_API)).FirstOrDefaultAsync();
    }

    public async Task RemoveManagementApiTokenAsync()
    {
        Token token = await GetManagementApiTokenAsync();
        if (token != null)
            context.Tokens.Remove(token);
    }
}
