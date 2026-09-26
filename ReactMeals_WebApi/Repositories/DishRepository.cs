using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ReactMeals_WebApi.Repositories;

public class DishRepository(MainDbContext context)
{
    public Task<bool> HasOrdersAsync(int dishId) =>
        context.OrderItems.AsNoTracking().AnyAsync(item => item.DishId == dishId);

    public Task<bool> IsImageUsedAsync(string fileName) =>
        context.Dishes.AsNoTracking().AnyAsync(dish => dish.Dish_url == fileName);
    public async Task AddAsync(Dish dish)
    {
        context.Dishes.Add(dish);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Dish dish)
    {
        var tracked = context.Dishes.Local.FirstOrDefault(d => d.DishId == dish.DishId);
        if (tracked != null)
        {
            context.Entry(tracked).CurrentValues.SetValues(dish);
        }
        else
        {
            context.Dishes.Update(dish);
        }
        await context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Dish dish)
    {
        var tracked = context.Dishes.Local.FirstOrDefault(d => d.DishId == dish.DishId);
        context.Dishes.Remove(tracked ?? dish);
        await context.SaveChangesAsync();
    }
}
