package gr.jimmys.jimmysfoodzilla.services.impl;

import gr.jimmys.jimmysfoodzilla.common.Result;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTO;
import gr.jimmys.jimmysfoodzilla.dto.AddDishDTOWithId;
import gr.jimmys.jimmysfoodzilla.models.Dish;
import gr.jimmys.jimmysfoodzilla.models.util.AddDishDTOMapping;
import gr.jimmys.jimmysfoodzilla.repository.DishRepository;
import gr.jimmys.jimmysfoodzilla.repository.OrderRepository;
import gr.jimmys.jimmysfoodzilla.services.api.DishImageService;
import gr.jimmys.jimmysfoodzilla.services.api.DishService;
import gr.jimmys.jimmysfoodzilla.services.api.DishesCacheService;
import gr.jimmys.jimmysfoodzilla.utils.Holder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.math.BigDecimal;
import java.util.Base64;
import java.util.UUID;

import static gr.jimmys.jimmysfoodzilla.common.ErrorMessages.*;

@Service
public class DishServiceImpl implements DishService {
    private static final Logger logger = LoggerFactory.getLogger(DishServiceImpl.class);
    private static final BigDecimal MAX_PRICE = BigDecimal.valueOf(256);
    private static final int MAX_IMAGE_BYTES = 5 * 1024 * 1024; // 5 MB
    private static final int MAX_B64_CHARS = 7_000_000;

    private final DishRepository dishRepository;
    private final OrderRepository orderRepository;
    private final DishesCacheService cache;
    private final DishImageService imageService;

    public DishServiceImpl(DishRepository dishRepository,
                           OrderRepository orderRepository,
                           DishesCacheService cache,
                           DishImageService imageService) {
        this.dishRepository = dishRepository;
        this.orderRepository = orderRepository;
        this.cache = cache;
        this.imageService = imageService;
    }

    private boolean isInvalidPrice(BigDecimal price) {
        return price == null || price.compareTo(BigDecimal.ZERO) <= 0
                || price.compareTo(MAX_PRICE) > 0
                || price.stripTrailingZeros().scale() > 2;
    }

    @Override
    public String generateDishFilename(String dishName, String dishB64, Holder<byte[]> imageBytes) {
        if (dishB64 == null || dishB64.isBlank() || dishB64.length() > MAX_B64_CHARS)
            return null;
        byte[] decoded;
        try {
            decoded = Base64.getDecoder().decode(dishB64.trim());
        } catch (IllegalArgumentException e) {
            return null;
        }
        if (decoded.length == 0 || decoded.length > MAX_IMAGE_BYTES)
            return null;
        var extension = imageService.validateImage(decoded);
        if (extension == null)
            return null;
        imageBytes.setValue(decoded);
        return UUID.randomUUID().toString().replace("-", "") + "." + extension;
    }

    @Override
    public Result<Dish> addDish(AddDishDTO dto) {
        if (dto == null || dto.getDishName() == null || dto.getDishName().isBlank())
            return Result.failure(BAD_DISH_NAME_REQUEST);
        if (isInvalidPrice(dto.getPrice()))
            return Result.failure(BAD_DISH_PRICE_REQUEST);
        if (cache.existDishByName(dto.getDishName().trim()))
            return Result.failure(CONFLICT);
        if (dto.getDishImageBase64() == null || dto.getDishImageBase64().isBlank())
            return Result.failure(BAD_REQUEST);

        var imageBytes = new Holder<byte[]>();
        var fileName = generateDishFilename(dto.getDishName(), dto.getDishImageBase64(), imageBytes);
        if (fileName == null)
            return Result.failure(BAD_REQUEST);

        var dish = AddDishDTOMapping.addDishDTOtoDish(dto);
        dish.setName(dto.getDishName().trim());
        dish.setUrl(fileName);

        try {
            imageService.saveImage(fileName, imageBytes.getValue());
            dish = dishRepository.save(dish);
        } catch (Exception ex) {
            logger.error("Failed to add dish, rolling back image file: {}", ex.getMessage());
            imageService.deleteImage(fileName);
            throw ex;
        }
        cache.addCacheEntry(dish);
        return Result.success(dish);
    }

    @Override
    public Result<Void> updateDish(AddDishDTOWithId dto) {
        if (dto == null || dto.getDishName() == null || dto.getDishName().isBlank())
            return Result.failure(BAD_DISH_NAME_REQUEST);
        var existingDish = cache.getDish(dto.getDishId());
        if (existingDish == null)
            return Result.failure(BAD_UPDATE_DISH_REQUEST);

        var sameName = cache.getDishByName(dto.getDishName().trim());
        if (sameName != null && sameName.getId() != dto.getDishId())
            return Result.failure(CONFLICT);

        if (isInvalidPrice(dto.getPrice()))
            return Result.failure(BAD_DISH_PRICE_REQUEST);

        String fileName = existingDish.getUrl();
        byte[] newImageBytes = null;

        if (dto.getDishImageBase64() != null && !dto.getDishImageBase64().isBlank()) {
            var imageBytes = new Holder<byte[]>();
            fileName = generateDishFilename(dto.getDishName(), dto.getDishImageBase64(), imageBytes);
            if (fileName == null)
                return Result.failure(BAD_REQUEST);
            newImageBytes = imageBytes.getValue();
        }

        var newDish = AddDishDTOMapping.addDishDTOWithIdtoDish(dto);
        newDish.setName(dto.getDishName().trim());
        newDish.setUrl(fileName);

        if (newImageBytes != null) {
            try {
                imageService.saveImage(fileName, newImageBytes);
                newDish = dishRepository.save(newDish);
            } catch (Exception ex) {
                logger.error("Failed to update dish image: {}", ex.getMessage());
                imageService.deleteImage(fileName);
                throw ex;
            }
            cache.updateCacheEntry(newDish);
            String oldUrl = existingDish.getUrl();
            if (oldUrl != null && !oldUrl.equalsIgnoreCase(fileName) && !dishRepository.existsByUrl(oldUrl)) {
                imageService.deleteImage(oldUrl);
            }
        } else {
            newDish = dishRepository.save(newDish);
            cache.updateCacheEntry(newDish);
        }

        return Result.success();
    }

    @Override
    public Result<Void> deleteDish(int id) {
        var dish = cache.getDish(id);
        if (dish == null)
            return Result.failure(NOT_FOUND);

        if (orderRepository.hasOrders(id))
            return Result.failure(CONFLICT);

        dishRepository.delete(dish);
        cache.deleteCacheEntry(id);

        if (dish.getUrl() != null && !dishRepository.existsByUrl(dish.getUrl())) {
            imageService.deleteImage(dish.getUrl());
        }

        return Result.success();
    }
}
