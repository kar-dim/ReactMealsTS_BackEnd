using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Repositories
{
    public class OrderItemRepository
    {
        private readonly MainDbContext _context;
        public OrderItemRepository(MainDbContext context)
        {
            _context = context;
        }
        public IQueryable<WebOrderItem> GetOrderItems()
        {
            return _context.OrderItems;
        }
    }
}
