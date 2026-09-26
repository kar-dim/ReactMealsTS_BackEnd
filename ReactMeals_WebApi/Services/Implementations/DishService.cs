using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services.Interfaces;

namespace ReactMeals_WebApi.Services.Implementations
{
    public class DishService(DishRepository dishRepo, IDishesCacheService cache, IDishImageService imageService) : IDishService
    {
        private string GenerateDishFilename(string dishB64, out byte[] imageBytes)
        {
            imageBytes = null;
            if (string.IsNullOrWhiteSpace(dishB64) || dishB64.Length > 7_000_000)
                return null;
            try
            {
                imageBytes = Convert.FromBase64String(dishB64);
            }
            catch (FormatException)
            {
                return null;
            }
            if (imageBytes.Length > 5_000_000)
                return null;
            string extension = imageService.ValidateImage(imageBytes);
            if (extension == null)
                return null;
            return $"{Guid.NewGuid():N}.{extension}";
        }

        //Create the dish, write to db
        public async Task<Result<Dish>> AddDishAsync(AddDishDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.DishName))
                return Result<Dish>.Failure(ErrorMessages.BadDishNameRequest);
            if (dto.Price <= 0 || dto.Price > 256 || decimal.Round(dto.Price, 2) != dto.Price)
                return Result<Dish>.Failure(ErrorMessages.BadDishPriceRequest);
            if (cache.GetDishByName(dto.DishName) != null)
                return Result<Dish>.Failure(ErrorMessages.Conflict);
            string fileName = GenerateDishFilename(dto.DishImageBase64, out byte[] imageBytes);
            if (fileName == null)
                return Result<Dish>.Failure(ErrorMessages.BadRequest);

            var dish = AddDishDTOMapping.AddDishDTOtoDish(dto);
            dish.Dish_url = fileName;

            try
            {
                imageService.SaveImage(fileName, imageBytes);
                await dishRepo.AddAsync(dish);
            }
            catch
            {
                imageService.DeleteImage(fileName);
                throw;
            }
            cache.AddCacheEntry(dish);

            return Result<Dish>.Success(dish);
        }

        //Update the dish, overwrite db entry
        public async Task<Result> UpdateDishAsync(AddDishDTOWithId dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.DishName))
                return Result.Failure(ErrorMessages.BadDishNameRequest);
            var existingDish = cache.GetDishById(dto.DishId);
            if (existingDish == null)
                return Result.Failure(ErrorMessages.BadUpdateDishRequest);
            var sameName = cache.GetDishByName(dto.DishName);
            if (sameName != null && sameName.DishId != dto.DishId)
                return Result.Failure(ErrorMessages.Conflict);
            if (dto.Price <= 0 || dto.Price > 256 || decimal.Round(dto.Price, 2) != dto.Price)
                return Result.Failure(ErrorMessages.BadDishPriceRequest);
            string fileName = existingDish.Dish_url;
            byte[] imageBytes = null;

            if (!string.IsNullOrWhiteSpace(dto.DishImageBase64))
            {
                fileName = GenerateDishFilename(dto.DishImageBase64, out imageBytes);
                if (fileName == null)
                    return Result.Failure(ErrorMessages.BadRequest);
            }



            var newDish = AddDishDTOMapping.AddDishDTOWithIdtoDish(dto);
            newDish.Dish_url = fileName;

            if (imageBytes != null)
            {
                try
            {
                imageService.SaveImage(fileName, imageBytes);
                await dishRepo.UpdateAsync(newDish);
            }
            catch
            {
                imageService.DeleteImage(fileName);
                throw;
            }
            cache.UpdateCacheEntry(newDish);
                if (!string.IsNullOrWhiteSpace(existingDish.Dish_url) &&
                    !string.Equals(existingDish.Dish_url, fileName, StringComparison.OrdinalIgnoreCase) &&
                    !await dishRepo.IsImageUsedAsync(existingDish.Dish_url))
                {
                    imageService.DeleteImage(existingDish.Dish_url);
                }
            }
            else
            {
                await dishRepo.UpdateAsync(newDish);
                cache.UpdateCacheEntry(newDish);
            }

            return Result.Success();
        }

        //Deletes the specified dish from the database and its image file from disk
        public async Task<Result> DeleteDishAsync(int id)
        {
            var dish = cache.GetDishById(id);
            if (dish == null)
                return Result.Failure($"Could not delete dish with ID {id}, it does not exist");

            if (await dishRepo.HasOrdersAsync(id))
                return Result.Failure(ErrorMessages.Conflict);

            try
            {
                await dishRepo.RemoveAsync(dish);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Result.Failure(ErrorMessages.Conflict);
            }
            cache.DeleteCacheEntry(id);
            if (!await dishRepo.IsImageUsedAsync(dish.Dish_url))
                imageService.DeleteImage(dish.Dish_url);

            return Result.Success();
        }
    }
}
