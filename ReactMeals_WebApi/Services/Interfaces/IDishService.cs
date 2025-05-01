using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;

namespace ReactMeals_WebApi.Services.Interfaces
{
    //Interface that defines dish operations
    public interface IDishService
    {
        public Task<Result> AddDishAsync(AddDishDTO dto);
        public Task<Result> UpdateDishAsync(AddDishDTOWithId dto);
        public Task<Result> DeleteDishAsync(int id);
    }
}
