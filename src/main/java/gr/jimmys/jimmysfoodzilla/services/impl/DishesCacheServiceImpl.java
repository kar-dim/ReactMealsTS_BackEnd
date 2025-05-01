package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.repository.DishRepository;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import jakarta.annotation.PostConstruct;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReadWriteLock;
import java.util.concurrent.locks.ReentrantReadWriteLock;

@Service
public class DishesCacheServiceImpl implements DishesCacheService {
    @Autowired
    private DishRepository dishRepository;

    private ArrayList<Dish> inMemoryDishes;

    private final Lock dishWriteLock;

    private final Lock dishReadLock;

    public DishesCacheServiceImpl() {
        //Dish cache ReadWrite lock stuff
        ReadWriteLock dishCacheLock = new ReentrantReadWriteLock();
        dishWriteLock = dishCacheLock.writeLock();
        dishReadLock = dishCacheLock.readLock();
    }

    @PostConstruct
    public void init() {
        inMemoryDishes = new ArrayList<>(dishRepository.findAllAscendingById());
    }

    @Override
    public Dish getDish(int dishId) {
        dishReadLock.lock();
        try {
            for (Dish dish : inMemoryDishes) {
                if (dish.getId() == dishId)
                    return dish;
            }
            return null;
        } finally {
            dishReadLock.unlock();
        }
    }

    @Override
    public List<Dish> getDishes() {
        dishReadLock.lock();
        try {
            return inMemoryDishes;
        } finally {
            dishReadLock.unlock();
        }
    }

    @Override
    public List<Dish> getDishes(List<Integer> dishIds) {
        Set<Integer> idSet = new HashSet<>(dishIds);
        dishReadLock.lock();
        try {
            return inMemoryDishes.stream()
                    .filter(dish -> idSet.contains(dish.getId()))
                    .toList();
        } finally {
            dishReadLock.unlock();
        }
    }

    @Override
    public void addCacheEntry(Dish dish) {
        dishWriteLock.lock();
        try {
            inMemoryDishes.add(dish);
        } finally {
            dishWriteLock.unlock();
        }
    }

    @Override
    public void deleteCacheEntry(int dishId) {
        dishWriteLock.lock();
        try {
            for (Dish dish : inMemoryDishes) {
                if (dish.getId() == dishId) {
                    inMemoryDishes.remove(dish);
                    break;
                }
            }
        } finally {
            dishWriteLock.unlock();
        }
    }

    @Override
    public BigDecimal getDishCost(int dishId) {
        dishReadLock.lock();
        try {
            return inMemoryDishes.stream()
                    .filter(dish -> dish.getId() == dishId)
                    .map(Dish::getPrice)
                    .findFirst()
                    .orElse(BigDecimal.ZERO); //assume ZERO is invalid value
        } finally {
            dishReadLock.unlock();
        }
    }

    @Override
    public void updateCacheEntry(Dish dish) {
        dishWriteLock.lock();
        try {
            for (int i = 0; i < inMemoryDishes.size(); i++) {
                if (inMemoryDishes.get(i).getId() == dish.getId()) {
                    inMemoryDishes.set(i, dish);
                    break;
                }
            }
        } finally {
            dishWriteLock.unlock();
        }
    }

    @Override
    public boolean existDishWithoutId(AddDishDTO dishToCheck) {
        dishReadLock.lock();
        try {
            for (Dish d : inMemoryDishes) {
                if (d.getName().equals(dishToCheck.getDishName())
                        && d.getUrl().equals(dishToCheck.getDishImageBase64())
                        && d.getDescription().equals(dishToCheck.getDishDescription())
                        && d.getExtendedInfo().equals(dishToCheck.getDishExtendedInfo()))
                    return true;
            }
            return false;
        } finally {
            dishReadLock.unlock();
        }
    }
}
