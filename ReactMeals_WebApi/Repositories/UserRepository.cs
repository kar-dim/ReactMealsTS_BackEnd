using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Repositories
{
    public class UserRepository
    {
        private readonly MainDbContext _context;
        public UserRepository(MainDbContext context)
        {
            _context = context;
        }

        public async Task<bool> UserExists(User user)
        {
            return await _context.Users.FirstOrDefaultAsync(user => user.User_Id == user.User_Id) != null;
        }
        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
    }
}
