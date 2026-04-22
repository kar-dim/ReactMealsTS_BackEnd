package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.repository.DishRepository;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import jakarta.persistence.EntityManager;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

/**
 * Copy-on-write cache: reads are lock-free (volatile field read),
 * writes are synchronized and replace the whole reference with a (new) unmodifiable list.
 */
@Service
public class DishesCacheServiceImpl implements DishesCacheService {

    private volatile List<Dish> inMemoryDishes;

    public DishesCacheServiceImpl(DishRepository dishRepository) {
        inMemoryDishes = List.copyOf(dishRepository.findAllAscendingById());
    }

    @Override
    public Dish getDish(int dishId) {
        return inMemoryDishes.stream()
                .filter(dish -> dish.getId() == dishId)
                .findFirst()
                .orElse(null);
    }

    @Override
    public List<Dish> getDishes() {
        return inMemoryDishes;
    }

    @Override
    public List<Dish> getDishes(List<Integer> dishIds, EntityManager em) {
        List<Dish> snapshot = inMemoryDishes;
        Set<Integer> idSet = new HashSet<>(dishIds);
        return snapshot.stream()
                .filter(dish -> idSet.contains(dish.getId()))
                .map(dish -> em.getReference(Dish.class, dish.getId()))
                .toList();
    }

    @Override
    public synchronized void addCacheEntry(Dish dish) {
        var newList = new ArrayList<>(inMemoryDishes);
        newList.add(dish);
        inMemoryDishes = List.copyOf(newList);
    }

    @Override
    public synchronized void deleteCacheEntry(int dishId) {
        inMemoryDishes = inMemoryDishes.stream()
                .filter(d -> d.getId() != dishId)
                .toList();
    }

    @Override
    public synchronized void updateCacheEntry(Dish dish) {
        inMemoryDishes = inMemoryDishes.stream()
                .map(d -> d.getId() == dish.getId() ? dish : d)
                .toList();
    }

    @Override
    public boolean existDishByName(String dishNameToCheck) {
        return inMemoryDishes.stream()
                .anyMatch(d -> d.getName().equalsIgnoreCase(dishNameToCheck));
    }
}
