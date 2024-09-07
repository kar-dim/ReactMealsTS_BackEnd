using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Services;

//Service that loads all dishes from db at startup
//Each web request that needs to read/write dishes, will access this dish cache instead of going to the db directly
//useful for bulk Get requests. After writing into the cache, controllers should immediately persist data into db
public class DishesCacheService : IHostedService, IDisposable
{
    private readonly MainDbContext _mainDbContext;
    private readonly List<Dish> _inMemoryDishes;
    private readonly ReaderWriterLockSlim _dishesCacheLock;
    private readonly CancellationTokenSource _cancellationTokenSource;
    public DishesCacheService(IServiceScopeFactory serviceScopeFactory)
    {
        _mainDbContext = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<MainDbContext>();
        _inMemoryDishes = new List<Dish>(_mainDbContext.Dishes.OrderBy(dish => dish.DishId));
        _dishesCacheLock = new ReaderWriterLockSlim();
        _cancellationTokenSource = new CancellationTokenSource();
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();
        await Task.CompletedTask;
    }

    public Dish GetDishById(int dishId)
    {
        _dishesCacheLock.EnterReadLock();
        try
        {
            return _inMemoryDishes.Find(dish => dish.DishId == dishId);
        }
        finally
        {
            _dishesCacheLock.ExitReadLock();
        }
    }

    public Dish GetDishByValues(AddDishDTO dish)
    {
        _dishesCacheLock.EnterReadLock();
        try
        {
            return _inMemoryDishes
            .Where(x => x.Dish_name.Equals(dish.Dish_name))
            .Where(x => x.Dish_description.Equals(dish.Dish_description))
            .Where(x => x.Price.Equals(dish.Price))
            .Where(x => x.Dish_extended_info.Equals(dish.Dish_extended_info)).FirstOrDefault();
        }
        finally
        {
            _dishesCacheLock.ExitReadLock();
        }
    }

    public (List<Dish>, int) GetDishes()
    {
        _dishesCacheLock.EnterReadLock();
        try
        {
            return (_inMemoryDishes, _inMemoryDishes.Count);
        }
        finally
        {
            _dishesCacheLock.ExitReadLock();
        }
    }

    public decimal GetDishCost(int dishId)
    {
        _dishesCacheLock.EnterReadLock();
        try
        {
            var dishInCache = _inMemoryDishes.Find(dish => dish.DishId == dishId);
            return dishInCache == null ? -1 : dishInCache.Price;
        }
        finally
        {
            _dishesCacheLock.ExitReadLock();
        }
    }

    public void AddCacheEntry(Dish dish)
    {
        _dishesCacheLock.EnterWriteLock();
        try
        {
            _inMemoryDishes.Add(dish);
        }
        finally
        {
            _dishesCacheLock.ExitWriteLock();
        }
    }

    public void DeleteCacheEntry(int dishId)
    {
        _dishesCacheLock.EnterWriteLock();
        try
        {
            Dish dishToRemove = _inMemoryDishes.FirstOrDefault(dish => dish.DishId == dishId);
            if (dishToRemove != null)
                _inMemoryDishes.Remove(dishToRemove);
        }
        finally
        {
            _dishesCacheLock.ExitWriteLock();
        }
    }

    public void UpdateCacheEntry(Dish dish)
    {
        _dishesCacheLock.EnterWriteLock();
        try
        {
            int dishIndex = _inMemoryDishes.FindIndex(d => d.DishId == dish.DishId);
            if (dishIndex != -1)
                _inMemoryDishes[dishIndex] = dish;
        }
        finally
        {
            _dishesCacheLock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _cancellationTokenSource?.Dispose();
    }
}
