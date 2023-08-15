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

        //protected endpoint for creating users, called from Auth0 server (Machine to Machine Authentication)
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

        //PUT api/Users/UpdateUser
        //only for Admins, to edit a User's details
        [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
        [HttpPut("UpdateUser")]
        public async Task<ActionResult<User>> UpdateUser([FromBody] User newUser)
        {
            //check if user is admin (role)
            //

            //todo, search in db and put the new values in db (if it does not exist -> 404)
            return Ok();
        }


        //DELETE api/Users/DeleteUser
        //only for Admins, to delete a User
        [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
        [HttpDelete("DeleteUser/{userId}")]
        public async Task<ActionResult<User>> DeleteUser(string userId)
        {
            //check if user is admin (role)
            //

            //todo, search in db (if exists -> return 404 not found user to delete)
            return Ok();
        }

    }
}
