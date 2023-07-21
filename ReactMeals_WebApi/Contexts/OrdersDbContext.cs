using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Contexts
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Dish> Dishes { get; set; }
    }
}
