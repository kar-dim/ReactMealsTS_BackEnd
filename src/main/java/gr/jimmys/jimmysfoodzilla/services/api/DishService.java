package gr.jimmys.jimmysfoodzilla.services.api;

import gr.jimmys.jimmysfoodzilla.common.Result;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTOWithId;
import gr.jimmys.jimmysfoodzilla.utils.Holder;

public interface DishService {
    String generateDishFilename(String dishName, String dishB64, Holder<byte[]> imageBytes);
    Result addDish(AddDishDTO dto);
    Result updateDish(AddDishDTOWithId dto);
    Result deleteDish(int id);
}
