using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishesController : ControllerBase
    {
        private readonly ILogger<DishesController> _logger;
        private readonly MainDbContext _mainDbContext;

        public DishesController(ILogger<DishesController> logger, MainDbContext ordersDbContext)
        {
            _mainDbContext = ordersDbContext;
            _logger = logger;
        }

        //GET api/Dish/GetDish/id
        [HttpGet("GetDish/{id:int}")]
        public async Task<ActionResult<Dish>> GetDish(long id)
        {

            Dish? foundDish = await (from x in _mainDbContext.Dishes where x.DishId == id select x).FirstOrDefaultAsync();
            if (foundDish is null)
            {
                _logger.LogError("Could not find dish with ID {0}", id);
                return NotFound();
            }
            _logger.LogInformation("Found dish with ID {0}", id);
            return Ok(foundDish);
            
        }

        //GET api/Dish/GetDishes
        [HttpGet("GetDishes")]
        public async Task<ActionResult<IEnumerable<Dish>>> GetDishes()
        {

            List<Dish> foundDishes = await _mainDbContext.Dishes.ToListAsync();
            if (foundDishes is null || foundDishes.Count == 0)
            {
                _logger.LogError("Could not find dishes");
                return NotFound();
            }
            _logger.LogInformation("Returned all dishes. Length: {0}", foundDishes.Count);
            return Ok(foundDishes);
        }

        //insert ORDER, body value:
        // order: ([dish1, posotita1], [dish2, posotita2],... userId)
        //must be logged in -> usage of Authorize attribute (auth0 jwt checks)
        [HttpPost("Order")]
        [Authorize(AuthenticationSchemes = "Default")]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] OrderDTO webOrder)
        {

            Console.WriteLine("ORDER RECEIVED!");

            if (webOrder is null || webOrder.order is null || webOrder.UserId is null || webOrder.order.Count == 0)
            {
                //wrong input data, something bad happened on CLIENT side -> 400
                return BadRequest();
            }
            List<Dish> dishList = await _mainDbContext.Dishes.ToListAsync();
            if (dishList is null)
            {
                //something bad happened on OUR side -> 500
                return Problem();
            }

            if (dishList.Count == 0)
            {
                //404
                return NotFound("At least one Dish ID does not exist!");
            }

            decimal cost = 0.0m;
            foreach (OrderItemDTO item in webOrder.order)
            {
                string dishName = "";
                bool idExistsInDb = false;
                //if no orders in DB -> no need to check anything
                foreach (Dish dish in dishList)
                {
                    if (item.DishId == dish.DishId) {
                        idExistsInDb = true;
                        dishName = dish.Dish_name;
                        cost += dish.Price;
                        break;
                    }
                }
                if (!idExistsInDb)
                {
                    //404
                    return NotFound("At least one Dish ID does not exist!");
                }
                Console.WriteLine("Dish Id: {0}, Dish NAME: {1}, Dish Counter: {2}", item.DishId, dishName, item.Dish_counter);
            }

            Order orderToInsert = OrderDTOMapping.DTOtoEntity(webOrder);
            orderToInsert.totalCost = cost;

            await _mainDbContext.AddAsync(orderToInsert);
            await _mainDbContext.SaveChangesAsync();
            //no errors -> 200 + empty body
            //TODO ->  201 + LOCATION REF HEADER the new obj? (there is no URI LOCATION though)
            return Ok();
        }
    }
}
