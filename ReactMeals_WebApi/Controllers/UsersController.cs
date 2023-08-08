using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        //private readonly OrdersDbContext _ordersDbContext;
        private readonly MainDbContext _mainDbContext;

        public UsersController(MainDbContext mainDbContext, ILogger<UsersController> logger)
        {
            _mainDbContext = mainDbContext;
            _logger = logger;
        }

        //protected endpoint for creating users
        [HttpPost("CreateUser")]
        [Authorize(AuthenticationSchemes = "M2M_UserRegister")]
        //param normally should be "UserDTO" but there is no need for DTO
        //(User is sent as-is from out Auth0 custom post register action as an Entity)
        public async Task<ActionResult<User>> CreateUser([FromBody] User userToCreate)
        {
            _logger.LogInformation("New User Created [Sent from Auth0]: " + userToCreate.ToString());
            if (await _mainDbContext.FindAsync<User>(new object[] { userToCreate.User_Id }) != null)
            {
                return Problem("User Already Exists!");
            }
            await _mainDbContext.AddAsync(userToCreate);
            await _mainDbContext.SaveChangesAsync();
            return Ok(userToCreate);
        }
    }
}
