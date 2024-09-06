using ReactMeals_WebApi.Contexts;
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
}
