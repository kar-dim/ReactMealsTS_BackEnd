using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ReactMeals_WebApi.Models
{
    public class Dish
    {
        [Key]
        public int DishId { get; set; }
        public string? Dish_name { get; set; }
        public string? Dish_description { get; set; }
        [Precision(18, 2)]
        public decimal Price { get; set; }
    }
}
