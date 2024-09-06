using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Repositories;

public class DishRepository(MainDbContext context)
{
    public async Task AddAsync(Dish dish)
    {
        context.Dishes.Add(dish);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Dish dish)
    {
        context.Dishes.Update(dish);
        await context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Dish dish)
    {
        context.Dishes.Remove(dish);
        await context.SaveChangesAsync();
    }
}
