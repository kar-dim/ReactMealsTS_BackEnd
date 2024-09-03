using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Services
{
    //Service that loads all dishes from db at startup
    //Each web request that needs to read/write dishes, will access this dish cache instead of going to the db directly
    //useful for bulk Get requests. After writing into the cache, controllers should immediately persist data into db
    public class DishesCacheService : IHostedService, IDisposable
    {
        private readonly List<Dish> _inMemoryDishes;
        private readonly ReaderWriterLockSlim dishesCacheLock;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public DishesCacheService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            dishesCacheLock = new ReaderWriterLockSlim();
            _cancellationTokenSource = new CancellationTokenSource();
            //get the scoped contextDb
            var mainDbContext = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<MainDbContext>();
            //populate cache
            _inMemoryDishes = new List<Dish>(mainDbContext.Dishes.OrderBy(dish => dish.DishId));
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

        public Dish GetDish(int dishId)
        {
            dishesCacheLock.EnterReadLock();
            try
            {
                return _inMemoryDishes.Find(dish => dish.DishId == dishId);
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
                return _inMemoryDishes;
            }
            finally
            {
                dishesCacheLock.ExitReadLock();
            }
        }

        public decimal GetDishCost(int dishId)
        {
            dishesCacheLock.EnterReadLock();
            try
            {
                var dishInCache = _inMemoryDishes.Find(dish => dish.DishId == dishId);
                return dishInCache == null ? -1 : dishInCache.Price; 
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
                _inMemoryDishes.Add(dish);
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
                Dish dishToRemove = _inMemoryDishes.FirstOrDefault(dish => dish.DishId == dishId);
                if (dishToRemove != null)
                    _inMemoryDishes.Remove(dishToRemove);
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
                int dishIndex = _inMemoryDishes.FindIndex(d => d.DishId == dish.DishId);
                if (dishIndex != -1)
                    _inMemoryDishes[dishIndex] = dish;
            }
            finally
            {
                dishesCacheLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _cancellationTokenSource?.Dispose();
        }
    }
}
