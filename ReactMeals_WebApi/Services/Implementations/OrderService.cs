using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services.Interfaces;

namespace ReactMeals_WebApi.Services.Implementations
{
    public class OrderService(IDishesCacheService cache, OrderRepository orderRepo) : IOrderService
    {
        //Create the order, write to db
        public async Task<Result> CreateOrderAsync(WebOrderDTO dto)
        {
            if (dto?.Order == null || dto.UserId == null || dto.Order.Count == 0)
                return Result.Failure("Invalid order data");

            //Get the cost of each dish from the cache
            var itemCosts = dto.Order.Select(item => new
                {
                    Item = item,
                    Cost = cache.GetDishCost(item.DishId)
                }).ToList();

            //check if all items are valid by checking the cost (from cache)
            var invalid = itemCosts.FirstOrDefault(x => x.Cost == null);
            if (invalid != null)
                return Result.Failure($"Invalid DishId: {invalid.Item.DishId}");

            //calculate total cost of each dish
            var totalCost = itemCosts.Sum(x => x.Cost.Value * x.Item.Dish_counter);
            await orderRepo.AddAsync(WebOrderDTOMapping.OrderDTOtoOrder(dto, totalCost));
            return Result.Success();
        }


        //Retrieve all the user's orders
        public async Task<UserOrdersDTO> GetUserOrdersAsync(string userId)
        {
            var orders = await orderRepo.GetUserOrdersAsync(userId);
            if (orders.Count == 0)
                return new UserOrdersDTO([]);
            var userOrders = orders
                .GroupBy(o => o.WebOrderId)
                .Select(group => new UserOrder(
                    group.Key,
                    [.. group.Select(g => new DishWithCounter(g.DishId, g.Dish_name, g.Dish_description, g.Price, g.Dish_counter))],
                    group.First().TotalCost))
                .ToArray();
            return new UserOrdersDTO(userOrders);
        }
    }
}
