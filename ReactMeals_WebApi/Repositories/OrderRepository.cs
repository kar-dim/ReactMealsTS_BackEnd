using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Repositories;

public class OrderRepository(MainDbContext context)
{
    public Task<bool> UserExistsAsync(string userId) =>
        context.Users.AsNoTracking().AnyAsync(user => user.User_Id == userId);
    public async Task AddAsync(WebOrder order)
    {
        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }
    public async Task<List<AllUserOrdersDTO>> GetUserOrdersAsync(string userId)
    {
        return await (from orderItem in context.OrderItems
                      join order in context.Orders on orderItem.WebOrderId equals order.Id
                      where order.UserId == userId
                      orderby order.Id descending, orderItem.Id ascending
                      select new AllUserOrdersDTO(order.TotalCost, orderItem.Id, orderItem.WebOrderId,
                      orderItem.DishId, orderItem.Dish_counter,
                      orderItem.Dish_name, orderItem.Dish_description, orderItem.Price)).AsNoTracking().ToListAsync();
    }
}
