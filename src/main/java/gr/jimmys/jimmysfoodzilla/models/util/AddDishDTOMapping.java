package gr.jimmys.jimmysfoodzilla.models.util;

import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTOWithId;
import gr.jimmys.jimmysfoodzilla.models.Dish;

public class AddDishDTOMapping {
    public static Dish addDishDTOtoDish(AddDishDTO addDishDTO) {
        return new Dish(0, addDishDTO.getDishName(), addDishDTO.getDishDescription(), addDishDTO.getPrice(), addDishDTO.getDishExtendedInfo(), null);
    }
    public static Dish addDishDTOWithIdtoDish(AddDishDTOWithId addDishDTO) {
        return new Dish(addDishDTO.getDishId(), addDishDTO.getDishName(), addDishDTO.getDishDescription(), addDishDTO.getPrice(), addDishDTO.getDishExtendedInfo(), null);
    }
}
