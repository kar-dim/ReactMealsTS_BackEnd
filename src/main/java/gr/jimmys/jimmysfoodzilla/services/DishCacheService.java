package gr.jimmys.jimmysfoodzilla.services;

import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.repository.DishRepository;
import gr.jimmys.jimmysfoodzilla.DTO.*;
import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReadWriteLock;
import java.util.concurrent.locks.ReentrantReadWriteLock;

@Service
public class DishCacheService {

    @Autowired
    private DishRepository dishRepository;
    private ArrayList<Dish> inMemoryDishes;
    private final Logger logger = LoggerFactory.getLogger(DishCacheService.class);

    //Dish cache ReadWrite lock stuff
    private final ReadWriteLock dishCacheLock;
    private final Lock dishWriteLock;
    private final Lock dishReadLock;

    public DishCacheService() {
        dishCacheLock = new ReentrantReadWriteLock();
        dishWriteLock = dishCacheLock.writeLock();
        dishReadLock = dishCacheLock.readLock();
    }

    @PostConstruct
    public void init() {
        inMemoryDishes = new ArrayList<>(dishRepository.findAllAscendingById());
    }

    public Dish getDish(int dishId)
    {
        dishReadLock.lock();
        try
        {
            for(Dish dish : inMemoryDishes)
            {
                if (dish.getDishId() == dishId)
                    return dish;
            }
            return null;
        }
        finally
        {
            dishReadLock.unlock();
        }
    }

    public List<Dish> getDishes()
    {
        dishReadLock.lock();
        try
        {
            return inMemoryDishes;
        }
        finally
        {
            dishReadLock.unlock();
        }
    }

    public void adddCacheEntry(Dish dish)
    {
        dishWriteLock.lock();
        try
        {
            inMemoryDishes.add(dish);
        }
        finally
        {
            dishWriteLock.unlock();
        }
    }

    public void deleteCacheEntry(int dishId)
    {
        dishWriteLock.lock();
        try
        {
            for (Dish dish : inMemoryDishes)
            {
                if (dish.getDishId() == dishId)
                {
                    inMemoryDishes.remove(dish);
                    break;
                }
            }
        }
        finally
        {
            dishWriteLock.unlock();
        }
    }

    public void updateCacheEntry(Dish dish)
    {
        dishWriteLock.lock();
        try
        {
            for (int i=0; i<inMemoryDishes.size(); i++)
            {
                if (inMemoryDishes.get(i).getDishId() == dish.getDishId())
                {
                    inMemoryDishes.set(i, dish);
                    break;
                }
            }
        }
        finally
        {
            dishWriteLock.unlock();
        }
    }

    public boolean existDishWithoutId(AddDishDTO dishToCheck) {
        dishReadLock.lock();
        try
        {
            for (Dish d : inMemoryDishes) {
                if (d.getDish_name().equals(dishToCheck.getDish_name())
                        && d.getDish_url().equals(dishToCheck.getDish_image_base64())
                        && d.getDish_description().equals(dishToCheck.getDish_description())
                        && d.getDish_extended_info().equals(dishToCheck.getDish_extended_info()))
                    return true;
            }
            return false;
        }
        finally
        {
            dishReadLock.unlock();
        }
    }
}
