using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services;
using System.Security.Claims;
using WebOrder = ReactMeals_WebApi.Models.WebOrder;

namespace ReactMeals_WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishesController : ControllerBase
    {
        private readonly OrderDbService _orderDbService;
        private readonly DishRepository _dishRepository;
        private readonly OrderRepository _orderRepository;
        private readonly ILogger<DishesController> _logger;
        private readonly ImageValidationService _imageValidationService;
        private readonly DishesCacheService _dishesCacheService;
        public DishesController(OrderDbService orderService, DishRepository dishRepository, OrderRepository orderRepository, ILogger<DishesController> logger, ImageValidationService imageValidationService, DishesCacheService dishesCacheService)
        {
            _orderDbService = orderService;
            _dishRepository = dishRepository;
            _orderRepository = orderRepository;
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
                _logger.LogError("GetDish: Could not find dish with ID {Id}", id);
                return NotFound();
            }
            _logger.LogInformation("GetDish: Found dish with ID {Id}",id);
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
            _logger.LogInformation("GetDishes: Returned all dishes. Length: {Length}", foundDishes.Item2);
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
            string extension = _imageValidationService.RetrieveImageExtension(imageBytes);
            if (extension == null)
            {
                _logger.LogError("AddDish: Invalid image data");
                return BadRequest("Invalid Image Data");
            }

            //now insert the dish into the db and receive the DishID returned
            string imageFileName = newDish.Dish_name.Trim().Replace(' ', '_').ToLower() + "." + extension;
            Dish newDishToAdd = AddDishDTOMapping.AddDishDTOtoDish(newDish);
            newDishToAdd.Dish_url = imageFileName;

            //add to cache and db
            _dishesCacheService.AddCacheEntry(newDishToAdd);
            await _dishRepository.AddAsync(newDishToAdd);
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
                _logger.LogError("UpdateDish: Dish With ID: {DishId} Not Found", newDish.DishId);
                return NotFound("Dish With ID: " + newDish.DishId + " Not Found");
            }
            //get old image url file
            string oldImageFileName = localDish.Dish_url;

            //get the base64 dish image data
            byte[] imageBytes = Convert.FromBase64String(newDish.Dish_image_base64);
            //some very basic validation (magic bytes)
            string extension = _imageValidationService.RetrieveImageExtension(imageBytes);
            if (extension == null)
            {
                _logger.LogError("UpdateDish: Invalid Image Data");
                return BadRequest("Invalid Image Data");
            }

            string imageFileName = newDish.Dish_name.Trim().Replace(' ', '_').ToLower() + "." + extension;
            Dish newDishToAdd = AddDishDTOMapping.AddDishDTOWithIdtoDish(newDish);
            newDishToAdd.Dish_url = imageFileName;

            //add to cache and db
            _dishesCacheService.UpdateCacheEntry(newDishToAdd);
            await _dishRepository.UpdateAsync(newDishToAdd);

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
                _logger.LogError("DeleteDish: Dish With ID: {Id} Not Found", id);
                return NotFound("Dish With ID: " + id + " Not Found");
            }

            //if found, remove from cache and db
            _dishesCacheService.DeleteCacheEntry(id);
            await _dishRepository.RemoveAsync(localDish);

            //remove static image
            try
            {
                System.IO.File.Delete(@"Images\" + localDish.Dish_url);
            }
            catch (Exception)
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
        public async Task<ActionResult<WebOrder>> CreateOrder([FromBody] WebOrderDTO webOrder)
        {
            Console.WriteLine("Order received");
            if (webOrder == null || webOrder.Order == null || webOrder.UserId == null || webOrder.Order.Count == 0)
            {
                //wrong input data, something bad happened on CLIENT side -> 400
                _logger.LogError("Order: Wrong Order Data");
                return BadRequest();
            }

            decimal totalCost = 0.0m;
            foreach (WebOrderItemDTO item in webOrder.Order)
            {
                //if no orders in DB -> no need to check anything
                decimal dishCost = _dishesCacheService.GetDishCost(item.DishId);
                if (dishCost == -1)
                {
                    _logger.LogError("Order: At least one Order Dish ID does not exist in DB");
                    return NotFound("At least one Dish ID does not exist!");
                }
                totalCost += dishCost * item.Dish_counter;
                _logger.LogInformation("Dish Id: {DishId}, Dish Counter: {Dish_counter}", item.DishId, item.Dish_counter);
            }

            WebOrder orderToInsert = WebOrderDTOMapping.OrderDTOtoOrder(webOrder);
            orderToInsert.TotalCost = totalCost;

            await _orderRepository.AddAsync(orderToInsert);
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
                _logger.LogError("GetUserOrders: Unauthorized User with userId: {UserId}", userId);
                return Unauthorized();
            }
            //search the OrderItem table to see if this user has any orders
            var allUserOrders = await _orderDbService.GetUserOrdersAsync(userId);
            if (allUserOrders.Count == 0)
                return Ok(new UserOrdersDTO(Array.Empty<UserOrder>())); //empty response -> user has no orders (technically not an error)

            List<UserOrder> userOrders = new List<UserOrder>();
            //each group is one order of a specific user
            foreach (var group in allUserOrders.GroupBy(x => x.WebOrderId))
            {
                DishWithCounter[] userOrderDishes = group
                    .Select(orderData => new DishWithCounter(orderData.DishId, orderData.Dish_name, orderData.Dish_description, orderData.Price, orderData.Dish_counter))
                    .ToArray();
                userOrders.Add(new UserOrder(group.Key, userOrderDishes, group.ToList()[0].TotalCost));
            }
            //send back the user's orders
            return Ok(new UserOrdersDTO(userOrders.ToArray()));
        }
    }
}
