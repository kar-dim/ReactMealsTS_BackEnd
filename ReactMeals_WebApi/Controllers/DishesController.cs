using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Services;
using System.Security.Claims;
using Order = ReactMeals_WebApi.Models.Order;

namespace ReactMeals_WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishesController : ControllerBase
    {
        private readonly ILogger<DishesController> _logger;
        private readonly MainDbContext _mainDbContext;
        private readonly IImageValidationService _imageValidationService;
        private readonly DishesCacheService _dishesCacheService;
        public DishesController(ILogger<DishesController> logger, IImageValidationService imageValidationService, MainDbContext ordersDbContext, DishesCacheService dishesCacheService)
        {
            _mainDbContext = ordersDbContext;
            _imageValidationService = imageValidationService;
            _logger = logger;
            _dishesCacheService = dishesCacheService;
        }

        //GET api/Dishes/GetDish/id
        //public method
        [HttpGet("GetDish/{id:int}")]
        public ActionResult<Dish> GetDish(int id)
        {
            Dish foundDish = _dishesCacheService.GetDishById(id);
            if (foundDish == null)
            {
                _logger.LogError("GetDish: Could not find dish with ID {0}", id);
                return NotFound();
            }
            _logger.LogInformation("GetDish: Found dish with ID {0}", id);
            return Ok(foundDish);
        }

        //GET api/Dishes/GetDishes
        //public method
        [HttpGet("GetDishes")]
        public ActionResult<IEnumerable<Dish>> GetDishes()
        {
            (List<Dish>, int) foundDishes = _dishesCacheService.GetDishes();
            if (foundDishes.Item2 == 0)
            {
                _logger.LogError("GetDishes: Could not find any dishes");
                return NotFound();
            }
            _logger.LogInformation("GetDishes: Returned all dishes. Length: {0}", foundDishes.Item2);
            return Ok(foundDishes.Item1);
        }

        //POST api/Dishes/AddDish
        //only for Admins, to add new dish to the database
        [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
        [HttpPost("AddDish")]
        public async Task<ActionResult<Dish>> AddDish([FromBody] AddDishDTO newDish)
        {
            //search in cache (if exists -> return 409 CONFLICT)
            //we don't have the ID yet, search by other parameters
            if (_dishesCacheService.GetDishByValues(newDish) != null)
            {
                _logger.LogError("AddDish: Dish already exists");
                return StatusCode(409, "Dish Already Exists");
            }
            //get the base64 dish image data
            byte[] imageBytes = Convert.FromBase64String(newDish.Dish_image_base64);
            //some very basic validation (magic bytes)
            string extension = _imageValidationService.IsValidImageMagicBytes(imageBytes);
            if (extension == null)
            {
                _logger.LogError("AddDish: Invalid image data");
                return BadRequest("Invalid Image Data");
            }

            //now insert the dish into the db and receive the DishID returned
            string imageFileName = newDish.Dish_name.Trim().Replace(' ', '_').ToLower() + "." + extension;
            Dish newDishToAdd = AddDishDTOMapping.DTOtoDish(newDish);
            newDishToAdd.Dish_url = imageFileName;

            //add to cache and db
            _dishesCacheService.AddCacheEntry(newDishToAdd);
            await _mainDbContext.AddAsync(newDishToAdd);
            await _mainDbContext.SaveChangesAsync();
            // Write image data to the static assets folder
            System.IO.File.WriteAllBytes(@"Images\" + imageFileName, imageBytes);

            //return the new dishId
            return Ok(newDishToAdd.DishId);
        }

        //PUT api/Dishes/UpdateDish
        //only for Admins, to edit a dish
        [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
        [HttpPut("UpdateDish")]
        public async Task<ActionResult<Dish>> UpdateDish([FromBody] AddDishDTOWithId newDish)
        {
            Dish localDish = _dishesCacheService.GetDishById(newDish.DishId);
            if (localDish == null)
            {
                _logger.LogError("UpdateDish: Dish With ID: " + newDish.DishId + " Not Found");
                return NotFound("Dish With ID: " + newDish.DishId + " Not Found");
            }
            //get old image url file
            string oldImageFileName = localDish.Dish_url;

            //get the base64 dish image data
            byte[] imageBytes = Convert.FromBase64String(newDish.Dish_image_base64);
            //some very basic validation (magic bytes)
            string extension = _imageValidationService.IsValidImageMagicBytes(imageBytes);
            if (extension == null)
            {
                _logger.LogError("UpdateDish: Invalid Image Data");
                return BadRequest("Invalid Image Data");
            }

            string imageFileName = newDish.Dish_name.Trim().Replace(' ', '_').ToLower() + "." + extension;
            Dish newDishToAdd = AddDishDTOMapping.DTOwithIdtoDish(newDish);
            newDishToAdd.Dish_url = imageFileName;

            //add to cache and db
            _dishesCacheService.UpdateCacheEntry(newDishToAdd);
            _mainDbContext.Dishes.Update(newDishToAdd);
            await _mainDbContext.SaveChangesAsync();
            //delete old image and create new file
            try
            {
                System.IO.File.Delete(@"Images\" + oldImageFileName);
                System.IO.File.WriteAllBytes(@"Images\" + imageFileName, imageBytes);
            }
            catch (Exception)
            {
                //it's ok, not something serious
                _logger.LogError("UpdateDish: Could not remove/update static dish image");
            }
            return Ok();
        }

        //DELETE api/Dishes/DeleteDish
        //only for Admins, to delete a dish
        [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
        [HttpDelete("DeleteDish/{id:int}")]
        public async Task<ActionResult<Dish>> DeleteDish(int id)
        {
            Dish localDish = _dishesCacheService.GetDishById(id);
            if (localDish == null)
            {
                _logger.LogError("DeleteDish: Dish With ID: " + id + " Not Found");
                return NotFound("Dish With ID: " + id + " Not Found");
            }

            //if found, remove from cache and db
            _dishesCacheService.DeleteCacheEntry(id);
            _mainDbContext.Dishes.Remove(localDish);
            await _mainDbContext.SaveChangesAsync();

            //remove static image
            try
            {
                System.IO.File.Delete(@"Images\" + localDish.Dish_url);
            } catch (Exception)
            {
                //it's ok, not something serious
                _logger.LogError("DeleteDish: Could not remove static dish image");
            }
            return Ok();
        }

        //insert ORDER, body value:
        // order: ([dish1, posotita1], [dish2, posotita2],... userId)
        //must be logged in -> usage of Authorize attribute (auth0 jwt checks)
        [HttpPost("Order")]
        [Authorize(AuthenticationSchemes = "Default")]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] OrderDTO webOrder)
        {
            Console.WriteLine("Order received");
            if (webOrder == null || webOrder.order == null || webOrder.UserId == null || webOrder.order.Count == 0)
            {
                //wrong input data, something bad happened on CLIENT side -> 400
                _logger.LogError("Order: Wrong Order Data");
                return BadRequest();
            }

            decimal totalCost = 0.0m;
            foreach (OrderItemDTO item in webOrder.order)
            {
                //if no orders in DB -> no need to check anything
                decimal dishCost = _dishesCacheService.GetDishCost(item.DishId);
                if (dishCost == -1)
                {
                    _logger.LogError("Order: At least one Order Dish ID does not exist in DB");
                    return NotFound("At least one Dish ID does not exist!");
                }
                totalCost += dishCost * item.Dish_counter;
                _logger.LogInformation("Dish Id: {0}, Dish Counter: {1}", item.DishId, item.Dish_counter);
            }

            Order orderToInsert = OrderDTOMapping.DTOtoEntity(webOrder);
            orderToInsert.totalCost = totalCost;

            await _mainDbContext.AddAsync(orderToInsert);
            await _mainDbContext.SaveChangesAsync();
            //no errors -> 200 + empty body
            //TODO ->  201 + LOCATION REF HEADER the new obj? (there is no URI LOCATION though)
            return Ok();
        }

        [HttpGet("GetUserOrders/{userId}")]
        [Authorize(AuthenticationSchemes = "Default")]
        public async Task<ActionResult<UserOrdersDTO>> GetUserOrders(string userId)
        {
            //check the user id of the token versus the one received from the web
            Claim nameClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (nameClaim == null || !nameClaim.Value.Equals(userId))
            {
                _logger.LogError("GetUserOrders: Unauthorized User with userId: "+userId);
                return Unauthorized();
            }
            //search the OrderItem table to see if this user has any orders
            var allUserOrdersQuery = from orderItem in _mainDbContext.OrderItems
                        join order in _mainDbContext.Orders on orderItem.OrderId equals order.Id
                        join dish in _mainDbContext.Dishes on orderItem.DishId equals dish.DishId
                        where order.UserId == userId
                        select new
                        {
                            orderItem.Id,
                            orderItem.OrderId,
                            order.totalCost,
                            orderItem.DishId,
                            orderItem.Dish_counter,
                            dish.Dish_name,
                            dish.Dish_description,
                            dish.Price
                        };

            var allUserOrders = await allUserOrdersQuery.ToListAsync();
            if (allUserOrders == null || allUserOrders.Count == 0)
            {
                return Ok(new UserOrdersDTO { orders = Array.Empty<UserOrder>() } ); //empty response -> user has no orders (technically not an error)
            }
            List<UserOrder> userOrderList = new List<UserOrder>();
            //split the list into sublists, each group is one order of a specific user
            foreach (var group in allUserOrders.GroupBy(x => x.OrderId))
            {
                int orderId = group.Key;
                var groupSubList = group.ToList();
                var tempList = new List<DishWithCounter>();
                foreach (var dish in groupSubList)
                {
                    tempList.Add(new DishWithCounter { 
                        DishId = dish.DishId, 
                        Dish_description = dish.Dish_description,
                        Dish_name = dish.Dish_name,
                        Price = dish.Price,
                        Dish_counter = dish.Dish_counter
                    }); 
                }
                userOrderList.Add(new UserOrder
                {
                    Id = orderId,
                    Dishes = tempList.ToArray(),
                    TotalCost = groupSubList[0].totalCost
                });
            }
            //send back the user's orders
            return Ok(new UserOrdersDTO { orders = userOrderList.ToArray() });
        }
    }
}
