package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.common.Result;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTOWithId;
import gr.jimmys.jimmysfoodzilla.models.util.AddDishDTOMapping;
import gr.jimmys.jimmysfoodzilla.repository.DishRepository;
import gr.jimmys.jimmysfoodzilla.services.api.DishImageService;
import gr.jimmys.jimmysfoodzilla.services.api.DishService;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import gr.jimmys.jimmysfoodzilla.utils.Holder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.util.Base64;

@Service
public class DishServiceImpl implements DishService {
    private final Logger logger = LoggerFactory.getLogger(DishServiceImpl.class);

    @Autowired
    DishRepository dishRepository;

    @Autowired
    DishesCacheService cache;

    @Autowired
    DishImageService imageService;

    @Override
    public String generateDishFilename(String dishName, String dishB64, Holder<byte[]> imageBytes) {
        imageBytes.setValue(Base64.getDecoder().decode(dishB64));
        var extension = imageService.validateImage(imageBytes.getValue());
        if (extension == null)
            return null;
        return dishName.trim().replace(' ', '_').toLowerCase() + "." + extension;
    }

    @Override
    public Result addDish(AddDishDTO dto) {
        if (cache.existDishWithoutId(dto))
            return Result.failure("Dish Already Exists");

        var imageBytes = new Holder<byte[]>();
        var fileName = generateDishFilename(dto.getDishName(), dto.getDishImageBase64(), imageBytes);
        if (fileName == null)
            return Result.failure("Invalid Image Data");
        var dish = AddDishDTOMapping.addDishDTOtoDish(dto);
        dish.setUrl(fileName);

        dish = dishRepository.save(dish);
        cache.addCacheEntry(dish);
        imageService.saveImage(fileName, imageBytes.getValue());

        return Result.success(dish);
    }

    @Override
    public Result updateDish(AddDishDTOWithId dto) {
        var existingDish = cache.getDish(dto.getDishId());
        if (existingDish == null)
            return Result.failure("Dish with ID " + dto.getDishId() + " not found");

        var imageBytes = new Holder<byte[]>();
        String fileName = generateDishFilename(dto.getDishName(), dto.getDishImageBase64(), imageBytes);
        if (fileName == null)
            return Result.failure("Invalid Image Data");

        var newDish = AddDishDTOMapping.addDishDTOWithIdtoDish(dto);
        newDish.setUrl(fileName);

        newDish = dishRepository.save(newDish); //will update
        cache.updateCacheEntry(newDish);
        imageService.replaceImage(existingDish.getUrl(), fileName, imageBytes.getValue());

        return Result.success();
    }

    @Override
    public Result deleteDish(int id) {
        var dish = cache.getDish(id);
        if (dish == null)
            return Result.failure("Could not delete dish with ID: " + id + ", it does not exist");

        cache.deleteCacheEntry(id);
        dishRepository.delete(dish);
        imageService.deleteImage(dish.getUrl());

        return Result.success();
    }
}
