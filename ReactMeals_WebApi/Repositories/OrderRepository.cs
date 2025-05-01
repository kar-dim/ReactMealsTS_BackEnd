using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Repositories;

public class OrderRepository(MainDbContext context)
{
    public async Task AddAsync(WebOrder order)
    {
        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }
    public IQueryable<WebOrder> GetOrders()
    {
        return context.Orders;
    }

    public async Task<List<AllUserOrdersDTO>> GetUserOrdersAsync(string userId)
    {
        return await (from orderItem in context.OrderItems
                      join order in context.Orders on orderItem.WebOrderId equals order.Id
                      join dish in context.Dishes on orderItem.DishId equals dish.DishId
                      where order.UserId == userId
                      select new AllUserOrdersDTO(order.TotalCost, orderItem.Id, orderItem.WebOrderId,
                      orderItem.DishId, orderItem.Dish_counter,
                      dish.Dish_name, dish.Dish_description, dish.Price)).AsNoTracking().ToListAsync();
    }
}
