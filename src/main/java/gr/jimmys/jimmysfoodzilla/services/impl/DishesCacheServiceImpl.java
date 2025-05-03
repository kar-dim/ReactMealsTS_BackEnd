package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.repository.DishRepository;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import jakarta.persistence.EntityManager;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantReadWriteLock;
import java.util.function.Supplier;

@Service
public class DishesCacheServiceImpl implements DishesCacheService {
    private final ArrayList<Dish> inMemoryDishes;

    private final Lock dishWriteLock;

    private final Lock dishReadLock;

    public DishesCacheServiceImpl(DishRepository dishRepository) {
        var dishCacheLock = new ReentrantReadWriteLock();
        dishWriteLock = dishCacheLock.writeLock();
        dishReadLock = dishCacheLock.readLock();
        inMemoryDishes = new ArrayList<>(dishRepository.findAllAscendingById());
    }

    private <T> T withDishReadLock(Supplier<T> supplier) {
        dishReadLock.lock();
        try {
            return supplier.get();
        }
        finally {
            dishReadLock.unlock();
        }
    }

    private void withDishWriteLock(Runnable action) {
        dishWriteLock.lock();
        try {
            action.run();
        }
        finally {
            dishWriteLock.unlock();
        }
    }

    @Override
    public Dish getDish(int dishId) {
        return withDishReadLock(() ->
                inMemoryDishes.stream()
                        .filter(dish -> dish.getId() == dishId)
                        .findFirst()
                        .orElse(null));
    }

    @Override
    public List<Dish> getDishes() {
        return withDishReadLock(() -> inMemoryDishes);
    }

    @Override
    public List<Dish> getDishes(List<Integer> dishIds, EntityManager em) {
        return withDishReadLock(() -> {
            Set<Integer> idSet = new HashSet<>(dishIds);
            return inMemoryDishes.stream()
                    .filter(dish -> idSet.contains(dish.getId()))
                    .map(dish -> em.getReference(Dish.class, dish.getId()))
                    .toList();
        });
    }

    @Override
    public void addCacheEntry(Dish dish) {
        withDishWriteLock(() -> inMemoryDishes.add(dish));
    }

    @Override
    public void deleteCacheEntry(int dishId) {
        withDishWriteLock(() -> inMemoryDishes.removeIf(d -> d.getId() == dishId));
    }

    @Override
    public void updateCacheEntry(Dish dish) {
        withDishWriteLock(() -> {
            for (int i = 0; i < inMemoryDishes.size(); i++) {
                if (inMemoryDishes.get(i).getId() == dish.getId()) {
                    inMemoryDishes.set(i, dish);
                    break;
                }
            }
        });
    }

    @Override
    public boolean existDishWithoutId(AddDishDTO dishToCheck) {
        return withDishReadLock(() -> inMemoryDishes.stream().anyMatch(d -> d.equalsExceptId(dishToCheck)));
    }
}
