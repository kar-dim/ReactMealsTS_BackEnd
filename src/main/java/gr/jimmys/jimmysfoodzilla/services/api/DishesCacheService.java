package gr.jimmys.jimmysfoodzilla.services.api;

import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import jakarta.persistence.EntityManager;

import java.math.BigDecimal;
import java.util.List;

public interface DishesCacheService {
    Dish getDish(int dishId);
    List<Dish> getDishes();
    List<Dish> getDishes(List<Integer> dishIds, EntityManager em);
    boolean existDishWithoutId(AddDishDTO dishToCheck);
    void addCacheEntry(Dish dish);
    void deleteCacheEntry(int dishId);
    void updateCacheEntry(Dish dish);
}
