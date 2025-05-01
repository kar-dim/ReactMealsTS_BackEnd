using ReactMeals_WebApi.Models;
using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.DTO;

//classes used by the "GetUserOrders" controller

public record UserOrder(int Id, DishWithCounter[] Dishes, decimal TotalCost);

public record UserOrdersDTO([property: JsonPropertyName("orders")] UserOrder[] Orders);
