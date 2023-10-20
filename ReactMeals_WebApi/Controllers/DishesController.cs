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
        public async Task<ActionResult<Dish>> GetDish(int id)
        {
            Dish? foundDish = _dishesCacheService.GetDish(id);
            if (foundDish == null)
            {
                _logger.LogError("GetDish: Could not find dish with ID {0}", id);
                return NotFound();
            }
            /*
            Dish? foundDish = await (from x in _mainDbContext.Dishes where x.DishId == id select x).FirstOrDefaultAsync();
            if (foundDish is null)
            {
                _logger.LogError("GetDish: Could not find dish with ID {0}", id);
                return NotFound();
            }
            */
            _logger.LogInformation("GetDish: Found dish with ID {0}", id);
            return Ok(foundDish);

        }

        //GET api/Dishes/GetDishes
        //public method
        [HttpGet("GetDishes")]
        public async Task<ActionResult<IEnumerable<Dish>>> GetDishes()
        {
            List<Dish> foundDishes = _dishesCacheService.GetDishes();
            if (foundDishes.Count == 0)
            {
                _logger.LogError("GetDishes: Could not find any dishes");
                return NotFound();
            }
            /*
            List<Dish> foundDishes = await _mainDbContext.Dishes.ToListAsync();
            if (foundDishes is null || foundDishes.Count == 0)
            {
                _logger.LogError("GetDishes: Could not find any dishes");
                return NotFound();
            }
            */
            _logger.LogInformation("GetDishes: Returned all dishes. Length: {0}", foundDishes.Count);
            return Ok(foundDishes);
        }


        //POST api/Dishes/AddDish
        //only for Admins, to add new dish to the database
        [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
        [HttpPost("AddDish")]
        public async Task<ActionResult<Dish>> AddDish([FromBody] AddDishDTO newDish)
        {
            //search in db(if exists-> return 409 CONFLICT)
            //we don't have the ID yet, search by other parameters
            /*
            var localDish = await _mainDbContext.Dishes
                .Where(x => x.Dish_name.Equals(newDish.Dish_name))
                .Where(x => x.Dish_description.Equals(newDish.Dish_description))
                .Where(x => x.Price.Equals(newDish.Price))
                .Where(x => x.Dish_extended_info.Equals(newDish.Dish_extended_info))
                .ToListAsync();
            */
            var localDish = _dishesCacheService.GetDishes()
                .Where(x => x.Dish_name.Equals(newDish.Dish_name))
                .Where(x => x.Dish_description.Equals(newDish.Dish_description))
                .Where(x => x.Price.Equals(newDish.Price))
                .Where(x => x.Dish_extended_info.Equals(newDish.Dish_extended_info)).ToList();

            if (localDish != null && localDish.Count > 0)
            {
                _logger.LogError("AddDish: Dish already exists");
                return StatusCode(409, "Dish Already Exists");
            }
            //get the base64 dish image data
            byte[] imageBytes = Convert.FromBase64String(newDish.Dish_image_base64);
            //some very basic validation (magic bytes)
            string? extension = _imageValidationService.IsValidImageMagicBytes(imageBytes);
            if (extension == null)
            {
                _logger.LogError("AddDish: Invalid image data");
                return BadRequest("Invalid Image Data");
            }

            //now insert the dish into the db and receive the DishID returned
            string imageFileName = newDish.Dish_name.Trim().Replace(' ', '_').ToLower() + "." + extension;
            Dish newDishToAdd = new Dish
            {
                Dish_name = newDish.Dish_name,
                Dish_description = newDish.Dish_description,
                Dish_extended_info = newDish.Dish_extended_info,
                Price = newDish.Price,
                Dish_url = imageFileName
            };

            //add to cache
            _dishesCacheService.AddCacheEntry(newDishToAdd);
            //insert to db
            await _mainDbContext.AddAsync(newDishToAdd);
            await _mainDbContext.SaveChangesAsync();

            //if all OK, put the image into "Images" static files folder
            string filePath = @"Images\" + imageFileName;
            // Write image data to the static assets folder
            System.IO.File.WriteAllBytes(filePath, imageBytes);

            //return the new dishId
            return Ok(newDishToAdd.DishId);
        }


        //PUT api/Dishes/UpdateDish
        //only for Admins, to edit a dish
        [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
        [HttpPut("UpdateDish")]
        public async Task<ActionResult<Dish>> UpdateDish([FromBody] AddDishDTOWithId newDish)
        {
            Dish? localDish = _dishesCacheService.GetDish(newDish.DishId);
            if (localDish == null)
            {
                _logger.LogError("UpdateDish: Dish With ID: " + newDish.DishId + " Not Found");
                return NotFound("Dish With ID: " + newDish.DishId + " Not Found");
            }
            /*
            //don't track this, it is used for "reading" only
            Dish? localDish = await _mainDbContext.Dishes.AsNoTracking().Where(x => x.DishId == newDish.DishId).FirstOrDefaultAsync();
            //if it does not exist -> 404)
            if (dishFromDb == null)
            {
                _logger.LogError("UpdateDish: Dish With ID: " + newDish.DishId + " Not Found");
                return NotFound("Dish With ID: " + newDish.DishId + " Not Found");
            }
            */
            //get old image url file
            string oldImageFileName = localDish.Dish_url;
            localDish = null;

            //get the base64 dish image data
            byte[] imageBytes = Convert.FromBase64String(newDish.Dish_image_base64);
            //some very basic validation (magic bytes)
            string? extension = _imageValidationService.IsValidImageMagicBytes(imageBytes);
            if (extension == null)
            {
                _logger.LogError("UpdateDish: Invalid Image Data");
                return BadRequest("Invalid Image Data");
            }

            string imageFileName = newDish.Dish_name.Trim().Replace(' ', '_').ToLower() + "." + extension;
            Dish newDishToAdd = new Dish
            {
                DishId = newDish.DishId, //set the ID so that update will work (else it will insert new row)
                Dish_name = newDish.Dish_name,
                Dish_description = newDish.Dish_description,
                Dish_extended_info = newDish.Dish_extended_info,
                Price = newDish.Price,
                Dish_url = imageFileName
            };

            //update cache
            _dishesCacheService.UpdateCacheEntry(newDishToAdd);
            //update db
            _mainDbContext.Dishes.Update(newDishToAdd);
            await _mainDbContext.SaveChangesAsync();

            //if all OK, handle static images
            string oldfilePath = @"Images\" + oldImageFileName;
            string newfilePath = @"Images\" + imageFileName;
            try
            {
                //remove OLD static image
                System.IO.File.Delete(oldfilePath);
                //put the new image into "Images" static files folder
                System.IO.File.WriteAllBytes(newfilePath, imageBytes);
            }
            catch (Exception ex)
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
            Dish? localDish = _dishesCacheService.GetDish(id);
            if (localDish == null)
            {
                _logger.LogError("DeleteDish: Dish With ID: " + id + " Not Found");
                return NotFound("Dish With ID: " + id + " Not Found");
            }
            /*
            Dish? localDish = await _mainDbContext.Dishes.Where(x => x.DishId == id).FirstOrDefaultAsync();
            if (localDish == null)
            {
                _logger.LogError("DeleteDish: Dish With ID: " + id + " Not Found");
                return NotFound("Dish With ID: " + id + " Not Found");
            }
            */
            string imageFileName = localDish.Dish_url;

            //if found, remove from cache
            _dishesCacheService.DeleteCacheEntry(id);
            //if found, remove it from db
            _mainDbContext.Dishes.Remove(localDish);
            await _mainDbContext.SaveChangesAsync();

            //remove static image
            try
            {
                System.IO.File.Delete(@"Images\" + imageFileName);
            } catch (Exception ex)
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

            if (webOrder is null || webOrder.order is null || webOrder.UserId is null || webOrder.order.Count == 0)
            {
                //wrong input data, something bad happened on CLIENT side -> 400
                _logger.LogError("Order: Wrong Order Data");
                return BadRequest();
            }
            List<Dish>? dishList = _dishesCacheService.GetDishes(); /*await _mainDbContext.Dishes.ToListAsync() */
            if (dishList is null || dishList.Count == 0)
            {
                //something bad happened on OUR side -> 500
                _logger.LogError("Order: NO dishes found!");
                return Problem("Internal Error");
            }

            decimal cost = 0.0m;
            foreach (OrderItemDTO item in webOrder.order)
            {
                string dishName = "";
                bool idExistsInDb = false;
                //if no orders in DB -> no need to check anything
                foreach (Dish dish in dishList)
                {
                    if (item.DishId == dish.DishId)
                    {
                        idExistsInDb = true;
                        dishName = dish.Dish_name;
                        cost += dish.Price * item.Dish_counter;
                        break;
                    }
                }
                if (!idExistsInDb)
                {
                    //404
                    _logger.LogError("Order: At least one Order Dish ID does not exist in DB");
                    return NotFound("At least one Dish ID does not exist!");
                }
                _logger.LogInformation("Dish Id: {0}, Dish NAME: {1}, Dish Counter: {2}", item.DishId, dishName, item.Dish_counter);
            }

            Order orderToInsert = OrderDTOMapping.DTOtoEntity(webOrder);
            orderToInsert.totalCost = cost;

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
            Claim? nameClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
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
                return Ok(new UserOrdersDTO { orders = new UserOrder[]{} } ); //empty response -> user has no orders (technically not an error)
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
                    TotalCost = (decimal)groupSubList[0].totalCost
                });
            }
            //send back the user's orders
            return Ok(new UserOrdersDTO { orders = userOrderList.ToArray() });
        }
    }
}
