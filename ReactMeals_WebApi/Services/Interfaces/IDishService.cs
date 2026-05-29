using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Services.Interfaces
{
    //Interface that defines dish operations
    public interface IDishService
    {
        public Task<Result<Dish>> AddDishAsync(AddDishDTO dto);
        public Task<Result> UpdateDishAsync(AddDishDTOWithId dto);
        public Task<Result> DeleteDishAsync(int id);
    }
}
