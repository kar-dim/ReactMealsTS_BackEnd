using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Services.Interfaces;
using System.Security.Claims;
using WebOrder = ReactMeals_WebApi.Models.WebOrder;

namespace ReactMeals_WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DishesController(ILogger<DishesController> logger, IDishesCacheService cache, IDishService dishService, IOrderService orderService) : ControllerBase
{
    //GET api/Dishes/GetDish/id
    //public method
    [HttpGet("GetDish/{id:int}")]
    public ActionResult<Dish> GetDish(int id)
    {
        var foundDish = cache.GetDishById(id);
        if (foundDish == null)
        {
            logger.LogError("GetDish: Could not find dish with ID {Id}", id);
            return NotFound(ErrorMessages.NotFound);
        }
        logger.LogInformation("GetDish: Found dish with ID {Id}", id);
        return Ok(foundDish);
    }

    //GET api/Dishes/GetDishes
    //public method
    [HttpGet("GetDishes")]
    public ActionResult<IEnumerable<Dish>> GetDishes()
    {
        List<Dish> foundDishes = cache.GetDishes();
        logger.LogInformation("GetDishes: Returned all dishes. Length: {Length}", foundDishes.Count);
        return Ok(foundDishes);
    }

    //POST api/Dishes/AddDish
    //only for Admins, to add new dish to the database
    [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
    [HttpPost("AddDish")]
    public async Task<IActionResult> AddDish([FromBody] AddDishDTO dto)
    {
        var result = await dishService.AddDishAsync(dto);
        if (!result.IsSuccess)
        {
            logger.LogError("AddDish failed: {Error}", result.Error);
            return result.Error switch
            {
                ErrorMessages.Conflict => Conflict(ErrorMessages.Conflict),
                ErrorMessages.BadDishPriceRequest or ErrorMessages.BadDishNameRequest => BadRequest(result.Error),
                _ => BadRequest(ErrorMessages.BadRequest),
            };
        }
        var dish = result.ResultValue as Dish;
        return Ok(dish.DishId);
    }

    //PUT api/Dishes/UpdateDish
    //only for Admins, to edit a dish
    [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
    [HttpPut("UpdateDish")]
    public async Task<ActionResult<Dish>> UpdateDish([FromBody] AddDishDTOWithId dto)
    {
        var result = await dishService.UpdateDishAsync(dto);
        if (!result.IsSuccess)
        {
            logger.LogError("UpdateDish failed: {Error}", result.Error);
            return result.Error switch
            {
                ErrorMessages.BadUpdateDishRequest => BadRequest(ErrorMessages.BadUpdateDishRequest),
                ErrorMessages.BadDishPriceRequest => BadRequest(ErrorMessages.BadDishPriceRequest),
                _ => BadRequest(ErrorMessages.BadRequest),
            };
        }
        return Ok();
    }

    //DELETE api/Dishes/DeleteDish
    //only for Admins, to delete a dish
    [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
    [HttpDelete("DeleteDish/{id:int}")]
    public async Task<ActionResult<Dish>> DeleteDish(int id)
    {
        var result = await dishService.DeleteDishAsync(id);
        return result.IsSuccess ? Ok() : NotFound(ErrorMessages.NotFound + $" Dish with ID {id} not found");
    }

    //insert ORDER, body value:
    // order: ([dish1, posotita1], [dish2, posotita2],... userId)
    //must be logged in -> usage of Authorize attribute (auth0 jwt checks)
    [HttpPost("Order")]
    [Authorize(AuthenticationSchemes = "Default")]
    public async Task<ActionResult<WebOrder>> CreateOrder([FromBody] WebOrderDTO dto)
    {
        var result = await orderService.CreateOrderAsync(dto);
        if (!result.IsSuccess)
        {
            logger.LogError("CreateOrder: {Error}", result.Error);
            return BadRequest(ErrorMessages.BadRequest + " " + result.Error);
        }
        return Ok();
    }

    [HttpGet("GetUserOrders/{userId}")]
    [Authorize(AuthenticationSchemes = "Default")]
    public async Task<ActionResult<UserOrdersDTO>> GetUserOrders(string userId)
    {
        var nameClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (nameClaim == null || nameClaim.Value != userId)
        {
            logger.LogError("GetUserOrders: Unauthorized access for user {UserId}", userId);
            return Unauthorized(ErrorMessages.Unauthorized);
        }
        var result = await orderService.GetUserOrdersAsync(userId);
        return Ok(result);
    }
}
