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
        return await context.Tokens.AsNoTracking().Where(token => token.TokenType.Equals(TokenType.MANAGEMENT_API)).FirstOrDefaultAsync();
    }

    public async Task ReplaceManagementApiTokenAsync(string tokenValue, DateTime expiryDate)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var existing = await context.Tokens.Where(t => t.TokenType.Equals(TokenType.MANAGEMENT_API)).FirstOrDefaultAsync();
            if (existing != null)
                context.Tokens.Remove(existing);
            context.Tokens.Add(new Token(tokenValue, TokenType.MANAGEMENT_API, expiryDate));
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
