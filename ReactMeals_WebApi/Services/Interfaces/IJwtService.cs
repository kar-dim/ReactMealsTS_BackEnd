using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Services.Interfaces;

// Interface that defines JWT (token) operations
public interface IJwtService
{
    public Task<Token> RenewToken();
}
