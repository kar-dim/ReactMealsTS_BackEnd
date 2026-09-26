using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Controllers;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services.Implementations;
using ReactMeals_WebApi.Services.Interfaces;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace TestProject;

public class FakeDishesCacheService : IDishesCacheService
{
    private readonly List<Dish> _dishes;

    public FakeDishesCacheService(List<Dish> dishes)
    {
        _dishes = dishes;
    }

    public void AddCacheEntry(Dish dish) => _dishes.Add(dish);
    public void DeleteCacheEntry(int dishId) => _dishes.RemoveAll(d => d.DishId == dishId);
    public Dish GetDishById(int dishId) => _dishes.FirstOrDefault(d => d.DishId == dishId);
    public Dish GetDishByName(string dishNameToCheck) => _dishes.FirstOrDefault(d => string.Equals(d.Dish_name, dishNameToCheck, StringComparison.OrdinalIgnoreCase));
    public decimal? GetDishCost(int dishId) => _dishes.FirstOrDefault(d => d.DishId == dishId)?.Price;
    public List<Dish> GetDishes() => _dishes;
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public void UpdateCacheEntry(Dish dish)
    {
        int idx = _dishes.FindIndex(d => d.DishId == dish.DishId);
        if (idx != -1) _dishes[idx] = dish;
    }
    public void Dispose() { }
}

public class FakeDishImageService : IDishImageService
{
    public List<string> SavedImages { get; } = new();
    public List<string> DeletedImages { get; } = new();

    public void DeleteImage(string fileName) => DeletedImages.Add(fileName);
    public void ReplaceImage(string oldFile, string newFile, byte[] data)
    {
        SavedImages.Add(newFile);
        DeletedImages.Add(oldFile);
    }
    public void SaveImage(string fileName, byte[] data) => SavedImages.Add(fileName);
    public string ValidateImage(byte[] imageData) => "jpg";
}

public class UnitTest1
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Test_AddDishDTOWithId_Deserialization()
    {
        string json = """
        {
            "dishId": 1,
            "dish_name": "Cheeseburger",
            "dish_description": "Tasty burger",
            "price": 10.50,
            "dish_extended_info": "More details",
            "dish_image_base64": null
        }
        """;

        var dto = JsonSerializer.Deserialize<AddDishDTOWithId>(json, JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal(1, dto.DishId);
        Assert.Equal("Cheeseburger", dto.DishName);
        Assert.Equal("Tasty burger", dto.DishDescription);
        Assert.Equal(10.50m, dto.Price);
        Assert.Equal("More details", dto.DishExtendedInfo);
        Assert.Null(dto.DishImageBase64);
    }

    [Fact]
    public void Test_AddDishDTOWithId_OmittedImage_Deserialization()
    {
        string json = """
        {
            "dishId": 1,
            "dish_name": "Cheeseburger",
            "dish_description": "Tasty burger",
            "price": 10.50,
            "dish_extended_info": "More details"
        }
        """;

        var dto = JsonSerializer.Deserialize<AddDishDTOWithId>(json, JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal(1, dto.DishId);
        Assert.Equal("Cheeseburger", dto.DishName);
        Assert.Null(dto.DishImageBase64);
    }

    [Fact]
    public void Test_WebOrderDTO_Deserialization()
    {
        string json = """
        {
            "order": [
                { "dishid": 1, "dish_counter": 2 },
                { "dishid": 2, "dish_counter": 3 }
            ]
        }
        """;

        var dto = JsonSerializer.Deserialize<WebOrderDTO>(json, JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal(2, dto.Order.Count);
        Assert.Equal(1, dto.Order[0].DishId);
        Assert.Equal(2, dto.Order[0].Dish_counter);
    }

    [Fact]
    public async Task Test_CreateOrderAsync_DeduplicatesDuplicateDishIds()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new MainDbContext(options);
        var testUser = new User("auth0|123", "test@test.com", "Test", "User", "Athens");
        var testDish = new Dish(1, "Cheeseburger", "Tasty burger", 10.00m) { Dish_url = "burger.jpg" };
        context.Users.Add(testUser);
        context.Dishes.Add(testDish);
        await context.SaveChangesAsync();

        var cache = new FakeDishesCacheService(new List<Dish> { testDish });
        var orderRepo = new OrderRepository(context);
        var orderService = new OrderService(cache, orderRepo);

        // Submit order with duplicate DishId 1 (2 + 3 = 5 items)
        var orderDto = new WebOrderDTO(new List<WebOrderItemDTO>
        {
            new WebOrderItemDTO(1, 2),
            new WebOrderItemDTO(1, 3)
        });

        var result = await orderService.CreateOrderAsync(orderDto, "auth0|123");

        Assert.True(result.IsSuccess);

        var savedOrder = await context.Orders.Include(o => o.Order).FirstOrDefaultAsync();
        Assert.NotNull(savedOrder);
        Assert.Equal(50.00m, savedOrder.TotalCost);
        Assert.Single(savedOrder.Order);
        var item = savedOrder.Order.First();
        Assert.Equal(1, item.DishId);
        Assert.Equal(5, item.Dish_counter);
        Assert.Equal("Cheeseburger", item.Dish_name);
        Assert.Equal(10.00m, item.Price);
    }

    [Fact]
    public async Task Test_UpdateDishAsync_AllowsNullImageAndPreservesExisting()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new MainDbContext(options);
        var originalDish = new Dish(1, "Original", "Desc", 15.00m, "Extended") { Dish_url = "original_guid.jpg" };
        context.Dishes.Add(originalDish);
        await context.SaveChangesAsync();

        var cache = new FakeDishesCacheService(new List<Dish> { originalDish });
        var dishRepo = new DishRepository(context);
        var imageService = new FakeDishImageService();
        var dishService = new DishService(dishRepo, cache, imageService);

        // Update dish without passing an image
        var updateDto = new AddDishDTOWithId(1, "Updated Name", "Updated Desc", 18.00m, "Updated Extended", null);
        var result = await dishService.UpdateDishAsync(updateDto);

        Assert.True(result.IsSuccess);
        Assert.Empty(imageService.DeletedImages); // existing image should NOT be deleted
        Assert.Empty(imageService.SavedImages); // no new image saved

        var cached = cache.GetDishById(1);
        Assert.Equal("Updated Name", cached.Dish_name);
        Assert.Equal("original_guid.jpg", cached.Dish_url); // preserved!
        Assert.Equal(18.00m, cached.Price);
    }

    [Fact]
    public void Test_DishesController_AttributesAndRoutes()
    {
        var method = typeof(DishesController).GetMethod(nameof(DishesController.CreateOrder));
        Assert.NotNull(method);
        var rateLimitAttr = method.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(rateLimitAttr);
        Assert.Equal("orderPolicy", rateLimitAttr.PolicyName);

        var getUserOrdersMethod = typeof(DishesController).GetMethod(nameof(DishesController.GetUserOrders));
        Assert.NotNull(getUserOrdersMethod);
        var httpGetAttrs = getUserOrdersMethod.GetCustomAttributes<HttpGetAttribute>().ToList();
        Assert.Single(httpGetAttrs);
        Assert.Equal("GetUserOrders", httpGetAttrs[0].Template);
    }

    [Fact]
    public async Task Test_GetUserOrdersAsync_ReturnsNewestFirst()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new MainDbContext(options);
        var testUser = new User("auth0|123", "test@test.com", "Test", "User", "Athens");
        var testDish = new Dish(1, "Cheeseburger", "Tasty burger", 10.00m) { Dish_url = "burger.jpg" };
        context.Users.Add(testUser);
        context.Dishes.Add(testDish);
        await context.SaveChangesAsync();

        var order1 = new WebOrder { Id = 1, UserId = "auth0|123", TotalCost = 10.00m, Order = new List<WebOrderItem> { new(1, 1) { Dish_name = "Cheeseburger", Price = 10.00m } } };
        var order2 = new WebOrder { Id = 2, UserId = "auth0|123", TotalCost = 20.00m, Order = new List<WebOrderItem> { new(1, 2) { Dish_name = "Cheeseburger", Price = 10.00m } } };
        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        var orderRepo = new OrderRepository(context);
        var orders = await orderRepo.GetUserOrdersAsync("auth0|123");

        Assert.Equal(2, orders.Count);
        Assert.Equal(2, orders[0].WebOrderId);
        Assert.Equal(1, orders[1].WebOrderId);
    }
}
