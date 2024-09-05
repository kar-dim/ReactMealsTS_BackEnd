using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Repositories
{
    public class DishRepository
    {
        private readonly MainDbContext _context;
        public DishRepository(MainDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Dish dish)
        {
            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Dish dish)
        {
            _context.Dishes.Update(dish);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(Dish dish)
        {
            _context.Dishes.Remove(dish);
            await _context.SaveChangesAsync();
        }
    }
}
