package gr.jimmys.jimmysfoodzilla.services.api;

import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.models.Dish;

import java.util.List;

public interface DishesCacheService {
    Dish getDish(int dishId);
    List<Dish> getDishes();
    void addCacheEntry(Dish dish);
    void deleteCacheEntry(int dishId);
    void updateCacheEntry(Dish dish);
    boolean existDishWithoutId(AddDishDTO dishToCheck);
}
