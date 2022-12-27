namespace ReactMeals_WebApi.Models
{
    public class Dish
    {
        public long Id { get; set; }
        public string? Dish_name { get; set; }
        public string? Dish_description { get; set; }
        public double Price { get; set; }
    }
}
