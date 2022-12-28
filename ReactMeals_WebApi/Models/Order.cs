namespace ReactMeals_WebApi.Models
{
    public class OrderItem
    {
        public int Dish_id { get; set; }
        public int Dish_counter { get; set; }
    }
    public class Order
    {
        public OrderItem[]? order { get; set; }   
    }
}
