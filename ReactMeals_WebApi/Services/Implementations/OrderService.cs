using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ReactMeals_WebApi.Services.Implementations
{
    public class OrderService(IDishesCacheService cache, OrderRepository orderRepo, ILogger<OrderService> logger = null) : IOrderService
    {
        private const int MaxOrderItemsCount = 100;
        private const decimal MaxOrderTotal = 9999999999999999.99m;

        //Create the order, write to db
        public async Task<Result> CreateOrderAsync(WebOrderDTO dto, string userId)
        {
            if (dto?.Order == null || string.IsNullOrWhiteSpace(userId) || dto.Order.Count == 0 || dto.Order.Count > MaxOrderItemsCount ||
                dto.Order.Any(item => item == null || item.Dish_counter <= 0 || item.DishId <= 0))
                return Result.Failure("Invalid order data");

            if (!await orderRepo.UserExistsAsync(userId))
                return Result.Failure("Registered user was not found");

            //Get the cost of each dish from the cache
            // Group duplicate dish IDs from client payload into single aggregated line items
            List<WebOrderItemDTO> aggregatedOrder;
            try
            {
                aggregatedOrder = [.. dto.Order
                    .GroupBy(item => item.DishId)
                    .Select(group => new WebOrderItemDTO(group.Key, checked(group.Sum(x => x.Dish_counter))))];
            }
            catch (OverflowException)
            {
                return Result.Failure("Order total is too large");
            }

            var sanitizedDto = dto with { Order = aggregatedOrder };

            var itemDishes = sanitizedDto.Order.Select(item => new
            {
                Item = item,
                Dish = cache.GetDishById(item.DishId)
            }).ToList();

            //check if all items are valid by checking the cost (from cache)
            var invalid = itemDishes.FirstOrDefault(x => x.Dish == null || x.Dish.Price <= 0 ||
                decimal.Round(x.Dish.Price, 2) != x.Dish.Price);
            if (invalid != null)
                return Result.Failure($"Invalid DishId: {invalid.Item.DishId}");

            //calculate total cost of each dish
            decimal totalCost;
            try
            {
                totalCost = itemDishes.Sum(x => checked(x.Dish.Price * x.Item.Dish_counter));
            }
            catch (OverflowException)
            {
                return Result.Failure("Order total is too large");
            }
            if (totalCost > MaxOrderTotal)
                return Result.Failure("Order total is too large");

            var order = WebOrderDTOMapping.OrderDTOtoOrder(sanitizedDto, totalCost, userId);
            foreach (var pair in order.Order.Zip(itemDishes))
            {
                var item = pair.First;
                var dish = pair.Second.Dish;
                item.Dish_name = dish.Dish_name;
                item.Dish_description = dish.Dish_description;
                item.Price = dish.Price;
            }
            try
            {
                await orderRepo.AddAsync(order);
            }
            catch (DbUpdateException ex)
            {
                logger?.LogError(ex, "Failed to persist order for user {UserId}: {ErrorMessage}. Inner: {InnerMessage}",
                    userId, ex.Message, ex.InnerException?.Message);
                return Result.Failure("Order references a user or dish that is no longer available");
            }
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
