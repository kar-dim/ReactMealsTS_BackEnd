package gr.jimmys.jimmysfoodzilla.services.api;

import gr.jimmys.jimmysfoodzilla.models.Dish;
import jakarta.persistence.EntityManager;

import java.util.List;

public interface DishesCacheService {
    Dish getDish(int dishId);
    List<Dish> getDishes();
    List<Dish> getDishes(List<Integer> dishIds, EntityManager em);
    boolean existDishByName(String dishNameToCheck);
    void addCacheEntry(Dish dish);
    void deleteCacheEntry(int dishId);
    void updateCacheEntry(Dish dish);
}
