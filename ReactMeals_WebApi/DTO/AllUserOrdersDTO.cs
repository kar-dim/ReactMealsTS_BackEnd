namespace ReactMeals_WebApi.DTO;
public record AllUserOrdersDTO(decimal TotalCost, int OrderItemId, int WebOrderId, int DishId, int Dish_counter, string Dish_name, string Dish_description, decimal Price);
