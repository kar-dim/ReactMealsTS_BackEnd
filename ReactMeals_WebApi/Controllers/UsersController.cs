using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services.Interfaces;
using RestSharp;
using System.Net;
using System.Text.Json;

using static System.String;

namespace ReactMeals_WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(UserRepository userRepository, RestClient client, ILogger<UsersController> logger, IJwtRenewalService jwtRenewalService) : ControllerBase
{
    //GET api/Users/GetUsers
    //only for Admins, to get a list of users
    [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
    [HttpGet("GetUsers")]
    public async Task<ActionResult<List<User>>> GetUsers()
    {
        //check ManagementAPI token if exists from the injected service
        string mApiToken = jwtRenewalService.ManagementApiToken;
        if (IsNullOrEmpty(mApiToken))
        {
            logger.LogError("ManagementAPI Token does not exist");
            return Problem(ErrorMessages.InternalError);
        }

        //send the request to auth0
        var request = new RestRequest("api/v2/users", Method.Get).AddHeader("Authorization", $"Bearer {mApiToken}");
        var response = await client.ExecuteAsync(request);
        if (response == null || response.StatusCode != HttpStatusCode.OK || IsNullOrEmpty(response.Content))
        {
            logger.LogError("Error in getting users info from api/v2/users");
            return Problem(ErrorMessages.InternalError);
        }

        var users = JsonSerializer.Deserialize<List<Auth0UserDeserialize>>(response.Content);
        List<User> usersToReturn = [];
        if (users == null || users.Count == 0)
        {
            logger.LogError("Users returned are malformed! Check Auth0 configuration");
            return Problem(ErrorMessages.InternalError);
        }

        foreach (var user in users)
        {
            //only send users that have defined values (else skip them entirely)
            if (!(user == null || IsNullOrEmpty(user.Email) || IsNullOrEmpty(user.UserId) || IsNullOrEmpty(user.UserMetadata.Name) || IsNullOrEmpty(user.UserMetadata.LastName) || IsNullOrEmpty(user.UserMetadata.Address)))
                usersToReturn.Add(new User(user.UserId, user.Email, user.UserMetadata.Name, user.UserMetadata.LastName, user.UserMetadata.Address));
        }
        return Ok(usersToReturn); //if empty it is still OK, client will handle it
    }

    //POST api/Users/CreateUser
    //used only by AUTH0 server
    [HttpPost("CreateUser")]
    [Authorize(AuthenticationSchemes = "M2M_UserRegister")]
    public async Task<ActionResult<User>> CreateUser([FromBody] User userToCreate)
    {
        if (await userRepository.UserExists(userToCreate))
        {
            logger.LogError("Error: User already exists");
            return Problem(ErrorMessages.InternalError);
        }
        logger.LogInformation("New User Created [Sent from Auth0]: {User}", userToCreate.ToString());
        await userRepository.AddAsync(userToCreate);
        return Ok(userToCreate);
    }

    //PUT api/Users/UpdateUser
    //only for Admins, to edit a User's details
    [Authorize(AuthenticationSchemes = "Default", Policy = "AdminPolicy")]
    [HttpPut("UpdateUser")]
    public async Task<ActionResult<User>> UpdateUser([FromBody] User newUser)
    {
        //check ManagementAPI token if exists from the injected service
        string mApiToken = jwtRenewalService.ManagementApiToken;
        if (IsNullOrEmpty(mApiToken))
        {
            logger.LogError("ManagementAPI Token does not exist");
            return Problem(ErrorMessages.InternalError);
        }

        //send the request to auth0 (HTTP PATCH) to update specific user data only
        var request = new RestRequest("api/v2/users/" + newUser.User_Id, Method.Patch)
            .AddHeader("Authorization", $"Bearer {mApiToken}")
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Accept", "application/json");
        string userJsonSerialize = JsonSerializer.Serialize(new Auth0UserSerialize(newUser.Email, new UserMetadata(newUser.Name, newUser.LastName, newUser.Address)));
        request.AddParameter("application/json", userJsonSerialize, ParameterType.RequestBody);
        var response = await client.ExecuteAsync(request);
        if (response == null || response.StatusCode != HttpStatusCode.OK || IsNullOrEmpty(response.Content))
        {
            logger.LogError("Error in patching user from api/v2/users");
            return Problem(ErrorMessages.InternalError);
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
        string mApiToken = jwtRenewalService.ManagementApiToken;
        if (IsNullOrEmpty(mApiToken))
        {
            logger.LogError("ManagementAPI Token does not exist");
            return Problem(ErrorMessages.InternalError);
        }

        //send the request to auth0 (HTTP DELETE) so that the User will be deleted from Auth0 servers
        var request = new RestRequest("api/v2/users/" + userId, Method.Delete).AddHeader("Authorization", $"Bearer {mApiToken}");
        var response = await client.ExecuteAsync(request);
        //DELETE OK status is 204
        if (response == null || response.StatusCode != HttpStatusCode.NoContent)
        {
            logger.LogError("Error in deleting user from api/v2/users");
            return Problem(ErrorMessages.InternalError);
        }
        return Ok();
    }
}
