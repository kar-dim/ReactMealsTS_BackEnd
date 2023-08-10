using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.DTO
{
    //classes used by the "GetUserOrders" controller
    public class UserOrder
    {
        public DishWithCounter[] Dishes { get; set; }
        public decimal TotalCost { get; set; }
    }
    public class UserOrdersDTO
    {
        public UserOrder[] orders { get; set; }
    }
}
