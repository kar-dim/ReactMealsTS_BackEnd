using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Models;
using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.DTO;

public record AddDishDTO
{
    [JsonPropertyName("dish_name")]
    public string DishName { get; init; }
    [JsonPropertyName("dish_description")]
    public string DishDescription { get; init; }
    [JsonPropertyName("price")]
    [Precision(18, 2)]
    public decimal Price { get; init; }
    [JsonPropertyName("dish_extended_info")]
    public string DishExtendedInfo { get; init; }
    [JsonPropertyName("dish_image_base64")]
    public string DishImageBase64 { get; init; }

    // Base constructor for AddDishDTO
    public AddDishDTO(string dishName, string dishDescription, decimal price, string dishExtendedInfo, string dishImageBase64)
    {
        DishName = dishName;
        DishDescription = dishDescription;
        Price = price;
        DishExtendedInfo = dishExtendedInfo;
        DishImageBase64 = dishImageBase64;
    }
}

public record AddDishDTOWithId(int DishId, string DishName, string DishDescription, decimal Price, string DishExtendedInfo, string DishImageBase64)
    : AddDishDTO(DishName, DishDescription, Price, DishExtendedInfo, DishImageBase64);

public class AddDishDTOMapping
{
    public static Dish AddDishDTOtoDish(AddDishDTO addDishDTO) =>
        new Dish(0, addDishDTO.DishName, addDishDTO.DishDescription, addDishDTO.Price, addDishDTO.DishExtendedInfo);
    public static Dish AddDishDTOWithIdtoDish(AddDishDTOWithId addDishDTO) =>
         new Dish(addDishDTO.DishId, addDishDTO.DishName, addDishDTO.DishDescription, addDishDTO.Price, addDishDTO.DishExtendedInfo);
}
