namespace ReactMeals_WebApi.DTO
{
    public class AllUserOrdersDTO(decimal totalCost, int orderItemId, int webOrderId, int dishId, int dishCounter, string dishName, string dishDescription, decimal price)
    {
        public decimal TotalCost { get; set; } = totalCost;
        public int OrderItemId { get; set; } = orderItemId;
        public int WebOrderId { get; set; } = webOrderId;
        public int DishId { get; set; } = dishId;
        public int Dish_counter { get; set; } = dishCounter;
        public string Dish_name { get; set; } = dishName;
        public string Dish_description { get; set; } = dishDescription;
        public decimal Price { get; set; } = price;
    }
}
