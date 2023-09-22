using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Services
{
    public class DishesCacheService : IHostedService, IDisposable
    {
        private string _className;
        private List<Dish> _inMemoryDishes;
        private readonly ReaderWriterLockSlim dishesCacheLock;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger<DishesCacheService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public DishesCacheService(ILogger<DishesCacheService> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            dishesCacheLock = new ReaderWriterLockSlim();
            _className = nameof(DishesCacheService) + ": ";
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = logger;
            _scopeFactory = scopeFactory;
            using (var scope = _scopeFactory.CreateScope())
            {
                //get the scoped contextDb
                var mainDbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

                _inMemoryDishes = new List<Dish>();
                var dishEntries = from dish in mainDbContext.Dishes
                                   orderby dish.DishId ascending
                                   select dish;

                //populate cache
                foreach (var dishEntry in dishEntries)
                {
                    _inMemoryDishes.Add(dishEntry);
                }
            }
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

        public Dish? GetDish(int dishId)
        {
            dishesCacheLock.EnterReadLock();
            try
            {
               foreach(Dish dish in _inMemoryDishes)
               {
                    if (dish.DishId == dishId)
                        return dish;
               }
               return null;
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
                foreach (Dish dish in _inMemoryDishes)
                {
                    if (dish.DishId == dishId)
                    {
                        _inMemoryDishes.Remove(dish);
                        break;
                    }
                }
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
                for (int i=0; i<_inMemoryDishes.Count; i++)
                {
                    if (_inMemoryDishes[i].DishId == dish.DishId)
                    {
                        _inMemoryDishes[i] = dish;
                        break;
                    }
                }
            }
            finally
            {
                dishesCacheLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}
