using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Repositories;

public class UserRepository(MainDbContext context)
{
    public async Task<bool> UserExists(User user)
    {
        return await context.Users.FirstOrDefaultAsync(localUser => localUser.User_Id == user.User_Id) != null;
    }
    public async Task AddAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User newUser)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.User_Id == newUser.User_Id) 
            ?? throw new InvalidOperationException($"User with ID {newUser.User_Id} does not exist.");
        existingUser.UpdateUser(newUser);
        await context.SaveChangesAsync();
    }
}
