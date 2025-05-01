using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services.Interfaces;

namespace ReactMeals_WebApi.Services.Implementations
{
    public class DishService(DishRepository dishRepo, IDishesCacheService cache, IDishImageService imageService, ILogger<DishService> logger) : IDishService
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
            if (cache.GetDishByValues(dto) != null)
                return Result.Failure("Dish already exists");

            string fileName = GenerateDishFilename(dto.DishName, dto.DishImageBase64, out byte[] imageBytes);
            if (fileName == null)
                return Result.Failure("Invalid Image Data");
            
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
                return Result.Failure($"Dish with ID {dto.DishId} not found");

            string fileName = GenerateDishFilename(dto.DishName, dto.DishImageBase64, out byte[] imageBytes);
            if (fileName == null)
                return Result.Failure("Invalid Image Data");

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

            try
            {
                File.Delete(Path.Combine("Images", dish.Dish_url));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete dish image file from disk, for dish with ID {Id}", id);
            }
            return Result.Success();
        }
    }
}
