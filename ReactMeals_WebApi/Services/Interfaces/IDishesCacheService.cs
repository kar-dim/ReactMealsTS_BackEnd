using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Services.Interfaces;

//Interface that defines dishes cache operations
public interface IDishesCacheService : IHostedService, IDisposable
{
    public Dish GetDishById(int dishId);
    public Dish GetDishByValues(AddDishDTO dish);
    public List<Dish> GetDishes();
    public decimal? GetDishCost(int dishId);
    public void AddCacheEntry(Dish dish);
    public void DeleteCacheEntry(int dishId);
    public void UpdateCacheEntry(Dish dish);
}
