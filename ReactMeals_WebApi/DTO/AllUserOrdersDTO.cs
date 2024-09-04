namespace ReactMeals_WebApi.DTO
{
    public class AllUserOrdersDTO
    {
        public decimal TotalCost { get; set; }
        public int OrderItemId { get; set; }
        public int WebOrderId { get; set; }
        public int DishId { get; set; }
        public int Dish_counter { get; set; }
        public string Dish_name { get; set; }
        public string Dish_description { get; set; }
        public decimal Price { get; set; }
        public AllUserOrdersDTO(decimal totalCost, int orderItemId, int webOrderId, int dishId, int dishCounter, string dishName, string dishDescription, decimal price)
        {
            TotalCost = totalCost;
            OrderItemId = orderItemId;
            WebOrderId = webOrderId;
            DishId = dishId;
            Dish_counter = dishCounter;
            Dish_name = dishName;
            Dish_description = dishDescription;
            Price = price;
        }
    }
}
