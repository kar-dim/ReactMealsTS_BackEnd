using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ReactMeals_WebApi.Models;

public class Dish
{
    public Dish() { }
    public Dish(int dishId, string dishName, string dishDescription, decimal price)
    {
        DishId = dishId;
        Dish_name = dishName;
        Dish_description = dishDescription;
        Price = price;
    }
    public Dish(int dishId, string dishName, string dishDescription, decimal price, string dishExtendedInfo)
    : this(dishId, dishName, dishDescription, price)
    {
        Dish_extended_info = dishExtendedInfo;
    }
    [Key]
    public int DishId { get; set; }
    public string Dish_name { get; set; }
    public string Dish_description { get; set; }
    [Precision(18, 2)]
    public decimal Price { get; set; }
    public string Dish_extended_info { get; set; }
    public string Dish_url { get; set; }
}

public class DishWithCounter : Dish
{
    public DishWithCounter() { }
    public DishWithCounter(int dishId, string dishName, string dishDescription, decimal price, int dishCounter)
        : base(dishId, dishName, dishDescription, price)
    {
        Dish_counter = dishCounter;
    }
    public int Dish_counter { get; set; }
}
