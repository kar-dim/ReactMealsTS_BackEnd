using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Models;
using System.ComponentModel.DataAnnotations;

namespace ReactMeals_WebApi.DTO
{
    public class AddDishDTO
    {
        public string? Dish_name { get; set; }
        public string? Dish_description { get; set; }
        [Precision(18, 2)]
        public decimal Price { get; set; }
        public string? Dish_extended_info { get; set; }
        public string Dish_image_base64 { get; set; } //base64 encoded image sent from client
    }

    public class AddDishDTOWithId : AddDishDTO
    {
        public int DishId { get; set; }
    }

    public class AddDishDTOMapping
    {
        public static Dish DTOtoEntity(AddDishDTO addDishDTO)
        {
            //Dish is incomplete after the DTO mapping,
            //must fill later dish_url and DishId returned from inserting into DB
            return new Dish
            {
                Dish_description = addDishDTO.Dish_description,
                Price = addDishDTO.Price,
                Dish_extended_info = addDishDTO.Dish_description,
                Dish_name = addDishDTO.Dish_name,
            };
        }
    }
}
