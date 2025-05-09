using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Services.Interfaces;

namespace ReactMeals_WebApi.Services.Implementations;

//Service that loads all dishes from db at startup
//Each web request that needs to read/write dishes, will access this dish cache instead of going to the db directly
//useful for bulk Get requests. After writing into the cache, controllers should immediately persist data into db
public class DishesCacheService : IDishesCacheService
{
    private readonly MainDbContext mainDbContext;
    private readonly List<Dish> inMemoryDishes;
    private readonly ReaderWriterLockSlim dishesCacheLock;

    public DishesCacheService(IServiceScopeFactory serviceScopeFactory)
    {
        mainDbContext = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<MainDbContext>();
        inMemoryDishes = [.. mainDbContext.Dishes.OrderBy(dish => dish.DishId)];
        dishesCacheLock = new ReaderWriterLockSlim();
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken) => await Task.CompletedTask;

    private T WithDishReadLock<T>(Func<T> func)
    {
        dishesCacheLock.EnterReadLock();
        try
        {
            return func();
        }
        finally
        {
            dishesCacheLock.ExitReadLock();
        }
    }

    private void WithDishWriteLock(Action action)
    {
        dishesCacheLock.EnterWriteLock();
        try
        {
            action();
        }
        finally
        {
            dishesCacheLock.ExitWriteLock();
        }
    }

    public Dish GetDishById(int dishId) => WithDishReadLock(() => inMemoryDishes.Find(dish => dish.DishId == dishId));

    public Dish GetDishByName(string dishNameToCheck) => WithDishReadLock(() => 
        inMemoryDishes.FirstOrDefault(x => string.Equals(x.Dish_name, dishNameToCheck, StringComparison.OrdinalIgnoreCase)));

    public List<Dish> GetDishes() => WithDishReadLock(() => inMemoryDishes);

    public decimal? GetDishCost(int dishId) => WithDishReadLock(() => inMemoryDishes.Find(dish => dish.DishId == dishId)?.Price);

    public void AddCacheEntry(Dish dish) => WithDishWriteLock(() => inMemoryDishes.Add(dish));

    public void DeleteCacheEntry(int dishId) => WithDishWriteLock(() =>
    {
        Dish dishToRemove = inMemoryDishes.FirstOrDefault(dish => dish.DishId == dishId);
        if (dishToRemove != null)
            inMemoryDishes.Remove(dishToRemove);
    });

    public void UpdateCacheEntry(Dish dish) => WithDishWriteLock(() =>
    {
        int dishIndex = inMemoryDishes.FindIndex(d => d.DishId == dish.DishId);
        if (dishIndex != -1)
            inMemoryDishes[dishIndex] = dish;
    });

    public void Dispose() => GC.SuppressFinalize(this);
}
