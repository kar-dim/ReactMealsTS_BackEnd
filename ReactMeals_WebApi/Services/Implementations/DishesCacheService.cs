using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Services.Interfaces;

namespace ReactMeals_WebApi.Services.Implementations;

//Service that caches dishes from db with a copy-on-write mechanism
//Each web request that needs to read/write dishes, will access this dish cache instead of going to the db directly
//useful for bulk Get requests. After writing into the cache, controllers should immediately persist data into db
public class DishesCacheService : IDishesCacheService
{
    private volatile List<Dish> _dishes;
    private readonly object _writeLock = new();

    public DishesCacheService(IServiceScopeFactory serviceScopeFactory)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        _dishes = [.. dbContext.Dishes.OrderBy(dish => dish.DishId)];
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Dish GetDishById(int dishId) => _dishes.Find(dish => dish.DishId == dishId);

    public Dish GetDishByName(string dishNameToCheck) =>
        _dishes.FirstOrDefault(x => string.Equals(x.Dish_name, dishNameToCheck, StringComparison.OrdinalIgnoreCase));

    public List<Dish> GetDishes() => _dishes;

    public decimal? GetDishCost(int dishId) => _dishes.Find(dish => dish.DishId == dishId)?.Price;

    public void AddCacheEntry(Dish dish)
    {
        lock (_writeLock)
            _dishes = [.. _dishes, dish];
    }

    public void DeleteCacheEntry(int dishId)
    {
        lock (_writeLock)
            _dishes = _dishes.Where(d => d.DishId != dishId).ToList();
    }

    public void UpdateCacheEntry(Dish dish)
    {
        lock (_writeLock)
        {
            var updated = _dishes.ToList();
            int idx = updated.FindIndex(d => d.DishId == dish.DishId);
            if (idx != -1) updated[idx] = dish;
            _dishes = updated;
        }
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
