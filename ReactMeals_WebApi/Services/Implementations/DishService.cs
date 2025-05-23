﻿using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services.Interfaces;

namespace ReactMeals_WebApi.Services.Implementations
{
    public class DishService(DishRepository dishRepo, IDishesCacheService cache, IDishImageService imageService) : IDishService
    {
        private string GenerateDishFilename(string dishName, string dishB64, out byte[] imageBytes)
        {
            imageBytes = Convert.FromBase64String(dishB64);
            string extension = imageService.ValidateImage(imageBytes);
            if (extension == null)
                return null;
            return dishName.Trim().Replace(' ', '_').ToLower() + "." + extension;
        }

        //Create the dish, write to db
        public async Task<Result> AddDishAsync(AddDishDTO dto)
        {
            if (dto.DishName == null)
                return Result.Failure(ErrorMessages.BadDishNameRequest);
            if (dto.Price <= 0 || dto.Price > 256)
                return Result.Failure(ErrorMessages.BadDishPriceRequest);
            if (cache.GetDishByName(dto.DishName) != null)
                return Result.Failure(ErrorMessages.Conflict);
            string fileName = GenerateDishFilename(dto.DishName, dto.DishImageBase64, out byte[] imageBytes);
            if (fileName == null)
                return Result.Failure(ErrorMessages.BadRequest);
            
            var dish = AddDishDTOMapping.AddDishDTOtoDish(dto);
            dish.Dish_url = fileName;

            cache.AddCacheEntry(dish);
            await dishRepo.AddAsync(dish);
            imageService.SaveImage(fileName, imageBytes);

            return Result.Success(dish);
        }

        //Update the dish, overwrite db entry
        public async Task<Result> UpdateDishAsync(AddDishDTOWithId dto)
        {
            var existingDish = cache.GetDishById(dto.DishId);
            if (existingDish == null)
                return Result.Failure(ErrorMessages.BadUpdateDishRequest + ": " + dto.DishId);
            if (dto.Price <= 0 || dto.Price > 256)
                return Result.Failure(ErrorMessages.BadDishPriceRequest);
            string fileName = GenerateDishFilename(dto.DishName, dto.DishImageBase64, out byte[] imageBytes);
            if (fileName == null)
                return Result.Failure(ErrorMessages.BadRequest);

            var newDish = AddDishDTOMapping.AddDishDTOWithIdtoDish(dto);
            newDish.Dish_url = fileName;

            cache.UpdateCacheEntry(newDish);
            await dishRepo.UpdateAsync(newDish);
            imageService.ReplaceImage(existingDish.Dish_url, fileName, imageBytes);

            return Result.Success();
        }

        //Deletes the specified dish from the database and its image file from disk
        public async Task<Result> DeleteDishAsync(int id)
        {
            var dish = cache.GetDishById(id);
            if (dish == null)
                return Result.Failure($"Could not delete dish with ID {id}, it does not exist");

            cache.DeleteCacheEntry(id);
            await dishRepo.RemoveAsync(dish);
            imageService.DeleteImage(dish.Dish_url);

            return Result.Success();
        }
    }
}
