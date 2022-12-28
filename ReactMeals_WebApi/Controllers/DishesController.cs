using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishesController : ControllerBase
    {
        //test
        private static Dish[] dishList = new Dish[5];
        static DishesController() {
            dishList[0] = new Dish { Id = 1, Dish_name = "Sushi", Dish_description = "Best sushi from japan!", Price = 8.37 };
            dishList[1] = new Dish { Id = 2, Dish_name = "Cheeseburger", Dish_description = "Hottest cheese and with the softest of buns!", Price = 2.30 };
            dishList[2] = new Dish { Id = 3, Dish_name = "Schnitzel", Dish_description = "So crispy! Yummmmmm", Price = 7.85 };
            dishList[3] = new Dish { Id = 4, Dish_name = "Greek Dolmadakia", Dish_description = "Traditional greek dish!", Price = 7.20 };
            dishList[4] = new Dish { Id = 5, Dish_name = "Pastitsio", Dish_description = "Traditional greek dish with Besamel", Price = 8.40 };
         }
       
        private readonly ILogger<DishesController> _logger;

        public DishesController(ILogger<DishesController> logger)
        {
            _logger = logger;
        }

        //GET api/Dish/GetDish/id
        [HttpGet("GetDish/{id:int}")]
        public ActionResult<Dish> GetDish(long id)
        {
            for (int i=0; i<dishList.Length; i++)
            {
                if (dishList[i].Id == id)
                {
                    _logger.LogInformation("Found dish with ID {0}", id);
                    return Ok(dishList[i]);
                }
            }
            _logger.LogError("Could not find dish with ID {0}", id);
            return NotFound();
        }

        //GET api/Dish/GetDishes
        [HttpGet("GetDishes")]
        public ActionResult<IEnumerable<Dish>> GetDishes()
        {
            _logger.LogInformation("Returned all dishes. Length: {0}", dishList.Length);
            return Ok(dishList);
        }

        //todo
        //insert ORDER, body value:
        // order: [dish1, posotita1], [dish2, posotita2],...
        [HttpPost("Order")]
        public ActionResult<Order> CreateOrder([FromBody] Order order)
        {
            Order webOrder = order;
            Console.WriteLine("ORDER RECEIVED!");
            //(kanonika thelei kai athroisma twn order items tis paraggelias gia na mpei sth vash)
            foreach(OrderItem item in webOrder.order)
            {
                string dishName = "";
                bool idExistsInDb = false;
                //psaksimo "sth vash"... (mhn ekane forge to request me fake data/id ktl?)
                foreach (Dish dish in dishList)
                {
                    if (item.Dish_id == dish.Id) {
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
            return Ok(webOrder);
        }
    }
}
