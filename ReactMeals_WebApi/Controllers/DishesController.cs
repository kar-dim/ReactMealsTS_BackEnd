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
            //test
            Dish[] dishList = new Dish[2];
            dishList[0] = new Dish { Id = 1, Description = "Best sushi from japan!", Name="Sushi", Price = 8.37 };
            dishList[1] = new Dish { Id = 2, Description = "Hottest cheese and with the softest of buns!", Name = "Cheeseburger", Price = 2.30 };
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
            //test
            Dish[] dishList = new Dish[2];
            dishList[0] = new Dish { Id = 1, Description = "Best sushi from japan!", Name = "Sushi", Price = 8.37 };
            dishList[1] = new Dish { Id = 2, Description = "Hottest cheese and with the softest of buns!", Name = "Cheeseburger", Price = 2.30 };
            _logger.LogInformation("Returned all dishes. Length: {0}", dishList.Length);
            return dishList;

            //return new Dish[] { dish1 };
            /*new Dish {
            {
                id: 3,
                dish_name: "Schnitzel",
                dish_description: "So crispy! Yummmmmm",
                price: 7.85
            },
            {
                 id: 4,
                 dish_name: "Greek Dolmadakia",
                 dish_description: "Traditional greek dish!",
                 price: 7.20
            },
            {
                 id: 5,
                 dish_name: "Pastitsio",
                 dish_description: "Traditional greek dish with Besamel",
                 price: 8.40
            } */
        }
    }
}
