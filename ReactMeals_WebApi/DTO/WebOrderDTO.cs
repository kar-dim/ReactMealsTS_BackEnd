using ReactMeals_WebApi.Models;
using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.DTO
{
    public class WebOrderItemDTO
    {
        public int DishId { get; set; }
        public int Dish_counter { get; set; }
    }

    public class WebOrderDTO
    {
        [JsonPropertyName("order")]
        public List<WebOrderItemDTO> Order { get; set; }
        public string UserId { get; set; }
    }

    public class WebOrderDTOMapping
    {
        public static WebOrder OrderDTOtoOrder(WebOrderDTO orderDTO)
        {
            var items = orderDTO.Order.Select(order => new WebOrderItem
            {
                DishId = order.DishId,
                Dish_counter = order.Dish_counter,
            }).ToList();
            return new WebOrder { Order = items, UserId = orderDTO.UserId };
        }
    }
}
