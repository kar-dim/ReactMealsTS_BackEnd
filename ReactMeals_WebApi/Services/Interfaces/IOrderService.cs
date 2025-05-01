using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;

namespace ReactMeals_WebApi.Services.Interfaces
{
    //Interface that defines order operations
    public interface IOrderService
    {
        public Task<Result> CreateOrderAsync(WebOrderDTO dto);
        public Task<UserOrdersDTO> GetUserOrdersAsync(string userId);
    }
}
