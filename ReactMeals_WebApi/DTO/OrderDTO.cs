using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.DTO
{
    public class OrderItemDTO
    {
        public int DishId { get; set; }
        public int Dish_counter { get; set; }
    }
    
    public class OrderDTO
    {
        public List<OrderItemDTO>? order { get; set; }
        public string UserId { get; set; }
    }

    public class OrderDTOMapping
    {
        public static Order DTOtoEntity(OrderDTO orderDTO)
        {
            ICollection<OrderItem> items = new List<OrderItem>();
            for (int i=0; i<orderDTO.order.Count; i++)
            {
                items.Add(new OrderItem()
                {
                    DishId = orderDTO.order[i].DishId,
                    Dish_counter = orderDTO.order[i].Dish_counter,
                });
            }
            return new Order() { order = items };
        }
    }
}
