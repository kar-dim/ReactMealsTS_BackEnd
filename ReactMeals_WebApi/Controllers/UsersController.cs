using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using RestSharp;
using System.Net;
using System.Text.Json;

namespace ReactMeals_WebApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly string _className;
        private readonly ILogger<UsersController> _logger;
        //private readonly OrdersDbContext _ordersDbContext;
        private readonly MainDbContext _mainDbContext;
        private readonly JwtValidationAndRenewalService _jwtValidationAndRenewalService;
        private readonly IConfiguration _configuration;

        public UsersController(MainDbContext mainDbContext, ILogger<UsersController> logger, JwtValidationAndRenewalService jwtValidationAndRenewalService, IConfiguration configuration)
        {
            _className = nameof(UsersController) + ": ";
            _mainDbContext = mainDbContext;
            _logger = logger;
            _jwtValidationAndRenewalService = jwtValidationAndRenewalService;
            _configuration = configuration;
        }

        //GET api/Users/GetUsers
        //only for Admins, to get a list of users
        [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
        [HttpGet("GetUsers")]
        public async Task<ActionResult<List<User>>> GetUsers()
        {
            //check ManagementAPI token if exists from the injected service
            string mApiToken = _jwtValidationAndRenewalService.ManagementApiAccessTokenValue;
            if (mApiToken.IsNullOrEmpty())
            {
                _logger.LogError(_className + "ManagementAPI Token does not exist");
                return Problem("Internal Problem");
            }

            //send the request to auth0
            RestClient client = new RestClient("https://" + _configuration["Auth0:M2M_Domain"]);
            RestRequest request = new RestRequest("api/v2/users", Method.Get);
            request.AddHeader("Authorization", $"Bearer {mApiToken}");
            var response = await client.ExecuteAsync(request);
            if (response == null || response.StatusCode != HttpStatusCode.OK || response.Content.IsNullOrEmpty())
            {
                _logger.LogError(_className + "Error in getting users info from api/v2/users");
                return Problem("INTERNAL ERROR");
            }

            List<Auth0UserDeserialize> users = JsonSerializer.Deserialize<List<Auth0UserDeserialize>>(response.Content);
            List<User> usersToReturn = new List<User>();
            if (users == null || users.Count == 0)
            {
                _logger.LogError(_className + "Users returned are malformed! Check Auth0 configuration");
                return Problem("INTERNAL ERROR");
            }

            foreach (Auth0UserDeserialize user in users)
            {
                //only send users that have defined values (else skip them entirely)
                if (!(user == null || user.email.IsNullOrEmpty() || user.user_id.IsNullOrEmpty() || user.user_metadata.name.IsNullOrEmpty() || user.user_metadata.last_name.IsNullOrEmpty() || user.user_metadata.address.IsNullOrEmpty()))
                {
                    //skip admin!
                    if (!user.nickname.Equals("admin"))
                    {
                        usersToReturn.Add(new User
                        {
                            User_Id = user.user_id,
                            Email = user.email,
                            Name = user.user_metadata.name,
                            LastName = user.user_metadata.last_name,
                            Address = user.user_metadata.address
                        });
                    }
                }
            }

            return Ok(usersToReturn); //if empty it is still OK, client will handle it
        }

        //POST api/Users/CreateUser
        //used only by AUTH0 server
        [HttpPost("CreateUser")]
        [Authorize(AuthenticationSchemes = "M2M_UserRegister")]
        public async Task<ActionResult<User>> CreateUser([FromBody] User userToCreate)
        {
            _logger.LogInformation(_className + "New User Created [Sent from Auth0]: " + userToCreate.ToString());
            if (await _mainDbContext.FindAsync<User>(new object[] { userToCreate.User_Id }) != null)
            {
                _logger.LogError(_className + "Error: User already exists");
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
            //check ManagementAPI token if exists from the injected service
            string mApiToken = _jwtValidationAndRenewalService.ManagementApiAccessTokenValue;
            if (mApiToken.IsNullOrEmpty())
            {
                _logger.LogError(_className + "ManagementAPI Token does not exist");
                return Problem("Internal Problem");
            }

            //send the request to auth0 (HTTP PATCH) to update specific user data only
            RestClient client = new RestClient("https://" + _configuration["Auth0:M2M_Domain"]);
            RestRequest request = new RestRequest("api/v2/users/" + newUser.User_Id, Method.Patch);
            request.AddHeader("Authorization", $"Bearer {mApiToken}");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");
            string userJsonSerialize = JsonSerializer.Serialize(new Auth0UserSerialize
            {
                email = newUser.Email,
                user_metadata = new UserMetadata
                {
                    name = newUser.Name,
                    last_name = newUser.LastName,
                    address = newUser.Address
                }
            });
            request.AddParameter("application/json", userJsonSerialize, ParameterType.RequestBody);
            var response = await client.ExecuteAsync(request);
            if (response == null || response.StatusCode != HttpStatusCode.OK || response.Content.IsNullOrEmpty())
            {
                _logger.LogError(_className + "Error in patching user from api/v2/users");
                return Problem("INTERNAL ERROR");
            }
            return Ok();
        }

        //DELETE api/Users/DeleteUser
        //only for Admins, to delete a User
        [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
        [HttpDelete("DeleteUser/{userId}")]
        public async Task<ActionResult<User>> DeleteUser(string userId)
        {
            //check ManagementAPI token if exists from the injected service
            string mApiToken = _jwtValidationAndRenewalService.ManagementApiAccessTokenValue;
            if (mApiToken.IsNullOrEmpty())
            {
                _logger.LogError(_className + "ManagementAPI Token does not exist");
                return Problem("Internal Problem");
            }

            //send the request to auth0 (HTTP DELETE) so that the User will be deleted from Auth0 servers
            RestClient client = new RestClient("https://" + _configuration["Auth0:M2M_Domain"]);
            RestRequest request = new RestRequest("api/v2/users/" + userId, Method.Delete);
            request.AddHeader("Authorization", $"Bearer {mApiToken}");
            var response = await client.ExecuteAsync(request);
            //DELETE OK status is 204
            if (response == null || response.StatusCode != HttpStatusCode.NoContent)
            {
                _logger.LogError(_className + "Error in deleting user from api/v2/users");
                return Problem("INTERNAL ERROR");
            }
            //we can delete the user's orders from our own db (Order, OrderItem tables)
            //but let's keep them for "archival/proof" reasons
            return Ok();
        }
    }
}
