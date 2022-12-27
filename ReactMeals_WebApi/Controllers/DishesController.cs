using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishesController : ControllerBase
    {

        private readonly ILogger<DishesController> _logger;

        public DishesController(ILogger<DishesController> logger)
        {
            _logger = logger;
        }

        //GET api/Dish/GetDish/id
        [HttpGet("GetDish/{id}")]
        public ActionResult<Dish> GetDish(long id)
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*"); //for localhost (test)
            //test
            Dish[] dishList = new Dish[5];
            dishList[0] = new Dish { Id = 1, Dish_name = "Sushi", Dish_description = "Best sushi from japan!", Price = 8.37 };
            dishList[1] = new Dish { Id = 2, Dish_name = "Cheeseburger", Dish_description = "Hottest cheese and with the softest of buns!", Price = 2.30 };
            dishList[2] = new Dish { Id = 3, Dish_name = "Schnitzel", Dish_description = "So crispy! Yummmmmm", Price = 7.85 };
            dishList[3] = new Dish { Id = 4, Dish_name = "Greek Dolmadakia", Dish_description = "Traditional greek dish!", Price = 7.20 };
            dishList[4] = new Dish { Id = 5, Dish_name = "Pastitsio", Dish_description = "Traditional greek dish with Besamel", Price = 8.40 };

            for (int i=0; i<dishList.Length; i++)
            {
                if (dishList[i].Id == id)
                {
                    _logger.LogInformation("Found dish with ID {0}", id);
                    return dishList[i];
                }
            }
            _logger.LogError("Could not find dish with ID {0}", id);
            return NotFound();
        }

        //GET api/Dish/GetDishes
        [HttpGet("GetDishes")]
        public ActionResult<IEnumerable<Dish>> GetDishes()
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*"); //for localhost (test)
            //test
            Dish[] dishList = new Dish[5];
            dishList[0] = new Dish { Id = 1, Dish_name = "Sushi", Dish_description = "Best sushi from japan!", Price = 8.37 };
            dishList[1] = new Dish { Id = 2, Dish_name = "Cheeseburger", Dish_description = "Hottest cheese and with the softest of buns!", Price = 2.30 };
            dishList[2] = new Dish { Id = 3, Dish_name = "Schnitzel", Dish_description = "So crispy! Yummmmmm", Price = 7.85 };
            dishList[3] = new Dish { Id = 4, Dish_name = "Greek Dolmadakia", Dish_description = "Traditional greek dish!", Price = 7.20 };
            dishList[4] = new Dish { Id = 5, Dish_name = "Pastitsio", Dish_description = "Traditional greek dish with Besamel", Price = 8.40 };

            _logger.LogInformation("Returned all dishes. Length: {0}", dishList.Length);
            return dishList;
        }
    }
}
