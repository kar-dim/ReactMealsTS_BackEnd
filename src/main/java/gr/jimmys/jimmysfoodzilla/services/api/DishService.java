package gr.jimmys.jimmysfoodzilla.services.api;

import gr.jimmys.jimmysfoodzilla.common.Result;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTOWithId;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.utils.Holder;

public interface DishService {
    String generateDishFilename(String dishName, String dishB64, Holder<byte[]> imageBytes);
    Result<Dish> addDish(AddDishDTO dto);
    Result<Void> updateDish(AddDishDTOWithId dto);
    Result<Void> deleteDish(int id);
}
