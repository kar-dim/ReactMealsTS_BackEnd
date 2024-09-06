using ReactMeals_WebApi.Models;
using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.DTO;

//classes used by the "GetUserOrders" controller
public class UserOrder
{
    public UserOrder(int id, DishWithCounter[] dishes, decimal totalCost)
    {
        Id = id;
        Dishes = dishes;
        TotalCost = totalCost;
    }
    public int Id { get; set; }
    public DishWithCounter[] Dishes { get; set; }
    public decimal TotalCost { get; set; }
}
public class UserOrdersDTO
{
    public UserOrdersDTO(UserOrder[] orders)
    {
        Orders = orders;
    }

    [JsonPropertyName("orders")]
    public UserOrder[] Orders { get; set; }
}
