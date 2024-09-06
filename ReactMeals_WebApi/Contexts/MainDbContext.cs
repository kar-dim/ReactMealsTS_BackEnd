using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Contexts;

public class MainDbContext : DbContext
{
    public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }
    public DbSet<WebOrderItem> OrderItems { get; set; }
    public DbSet<WebOrder> Orders { get; set; }
    public DbSet<Dish> Dishes { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Token> Tokens { get; set; }
}
