using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Services;

namespace ReactMeals_WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishesController : ControllerBase
    {
        private readonly ILogger<DishesController> _logger;
        private readonly JimmysFoodzillaService _dbService;

        public DishesController(JimmysFoodzillaService dbService, ILogger<DishesController> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        //GET api/Dish/GetDish/id
        [HttpGet("GetDish/{id:int}")]
        public async Task<ActionResult<Dish>> GetDish(long id)
        {
            Dish foundDish = await _dbService.GetDishAsync(id);
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
            List<Dish> foundDishes = await _dbService.GetDishesAsync();
            if (foundDishes is null || foundDishes.Count == 0)
            {
                _logger.LogError("Could not find dishes");
                return NotFound();
            }
            _logger.LogInformation("Returned all dishes. Length: {0}", foundDishes.Count);
            return Ok(foundDishes);
        }

        //todo
        //insert ORDER, body value:
        // order: [dish1, posotita1], [dish2, posotita2],...
        [HttpPost("Order")]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] Order webOrder)
        { 
            Console.WriteLine("ORDER RECEIVED!");

            if (webOrder is null || webOrder.order.Length == 0)
            {
                //wrong input data, something bad happened on CLIENT side -> 400
                return BadRequest();
            }
            List<Dish> dishList = await _dbService.GetDishesAsync();
            if (dishList is null)
            {
                //something bad happened on OUR side -> 500
                return Problem();
            }

            if (dishList.Count == 0)
            {
                //404
                return NotFound("At least on Dish ID does not exist!");
            }

            //(kanonika thelei kai athroisma twn order items tis paraggelias gia na mpei sth vash)
            foreach (OrderItem item in webOrder.order)
            {
                string dishName = "";
                bool idExistsInDb = false;
                //if no orders in DB -> no need to check anything
                foreach (Dish dish in dishList)
                {
                    if (item.Dish_id == dish.Dish_id) {
                        idExistsInDb = true;
                        dishName = dish.Dish_name;
                        break;
                    }
                }
                if (!idExistsInDb)
                {
                    //404
                    return NotFound("At least on Dish ID does not exist!");
                }
                Console.WriteLine("Dish Id: {0}, Dish NAME: {1}, Dish Counter: {2}", item.Dish_id, dishName, item.Dish_counter);
            }
            //no errors -> tha steilei 200 + sto body to webOrder (praktika to idio object poy mas esteile)
            //TODO -> isws na steiloume 201 me LOCATION REF HEADER to new obj? (omws aplws mpainei sth VASH, den yparxei kapoy sto sait gia reference)
            return Ok(webOrder);
        }
    }
}
