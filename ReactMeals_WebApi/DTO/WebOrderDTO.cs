using ReactMeals_WebApi.Models;
using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.DTO;

public record WebOrderItemDTO(int DishId, int Dish_counter);

public record WebOrderDTO([property: JsonPropertyName("order")] List<WebOrderItemDTO> Order, string UserId);

public class WebOrderDTOMapping
{
    public static WebOrder OrderDTOtoOrder(WebOrderDTO orderDTO, decimal totalCost)
    {
        var items = orderDTO.Order.Select(order => new WebOrderItem(order.DishId, order.Dish_counter)).ToList();
        return new WebOrder { Order = items, UserId = orderDTO.UserId, TotalCost = totalCost };
    }
}
