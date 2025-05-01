using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.DTO;
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
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    public Dish GetDishById(int dishId)
    {
        dishesCacheLock.EnterReadLock();
        try
        {
            return inMemoryDishes.Find(dish => dish.DishId == dishId);
        }
        finally
        {
            dishesCacheLock.ExitReadLock();
        }
    }

    public Dish GetDishByValues(AddDishDTO dish)
    {
        dishesCacheLock.EnterReadLock();
        try
        {
            return inMemoryDishes.FirstOrDefault(x =>
            x.Dish_name == dish.DishName &&
            x.Dish_description == dish.DishDescription &&
            x.Price == dish.Price &&
            x.Dish_extended_info == dish.DishExtendedInfo);
        }
        finally
        {
            dishesCacheLock.ExitReadLock();
        }
    }

    public List<Dish> GetDishes()
    {
        dishesCacheLock.EnterReadLock();
        try
        {
            return inMemoryDishes;
        }
        finally
        {
            dishesCacheLock.ExitReadLock();
        }
    }

    public decimal? GetDishCost(int dishId)
    {
        dishesCacheLock.EnterReadLock();
        try
        {
           return inMemoryDishes.Find(dish => dish.DishId == dishId)?.Price;
        }
        finally
        {
            dishesCacheLock.ExitReadLock();
        }
    }

    public void AddCacheEntry(Dish dish)
    {
        dishesCacheLock.EnterWriteLock();
        try
        {
            inMemoryDishes.Add(dish);
        }
        finally
        {
            dishesCacheLock.ExitWriteLock();
        }
    }

    public void DeleteCacheEntry(int dishId)
    {
        dishesCacheLock.EnterWriteLock();
        try
        {
            Dish dishToRemove = inMemoryDishes.FirstOrDefault(dish => dish.DishId == dishId);
            if (dishToRemove != null)
                inMemoryDishes.Remove(dishToRemove);
        }
        finally
        {
            dishesCacheLock.ExitWriteLock();
        }
    }

    public void UpdateCacheEntry(Dish dish)
    {
        dishesCacheLock.EnterWriteLock();
        try
        {
            int dishIndex = inMemoryDishes.FindIndex(d => d.DishId == dish.DishId);
            if (dishIndex != -1)
                inMemoryDishes[dishIndex] = dish;
        }
        finally
        {
            dishesCacheLock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
