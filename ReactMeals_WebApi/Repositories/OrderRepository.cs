using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Repositories
{
    public class OrderRepository
    {
        private readonly MainDbContext _context;
        public OrderRepository(MainDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(WebOrder order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
        }
        public IQueryable<WebOrder> GetOrders()
        {
            return _context.Orders;
        }
    }
}
