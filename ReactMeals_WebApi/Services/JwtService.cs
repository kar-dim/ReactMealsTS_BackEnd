using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Repositories;
using RestSharp;
using System.Net;
using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.Services;

public class Auth0ManagementResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonPropertyName("scope")]
    public string Scope { get; set; }
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }
}
public class JwtService(IServiceScopeFactory serviceScopeFactory, ILogger<JwtService> logger, IConfiguration configuration, RestClient client)
{
    private readonly TokenRepository tokenRepository = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<TokenRepository>();
    private readonly string M2MSecret = File.ReadAllText("m2m_secret.txt").Trim();

    public async Task<(bool, DateTime?, string)> IsTokenExpired()
    {
        //check if the current token is expired
        Token tokenFromDb = await tokenRepository.GetManagementApiTokenAsync();
        if (tokenFromDb == null)
        {
            logger.LogInformation("No ManagementAPI Token found in db, fetching new...");
            return (true, null, string.Empty); //no token found
        }
        return (tokenFromDb.ExpiryDate <= DateTime.Now, tokenFromDb.ExpiryDate, tokenFromDb.TokenValue);
    }

    //call the Auth0 Rest service to renew the token
    public async Task<(DateTime, bool, string)> RenewToken()
    {
        RestRequest request = new RestRequest("oauth/token", Method.Post);
        request.AddHeader("content-type", "application/json");
        request.AddParameter("application/x-www-form-urlencoded", "{\"client_id\":\"" + configuration["Auth0:M2M_ClientID"] + "\",\"client_secret\":\"" + M2MSecret + "\",\"audience\":\"" + "https://" + configuration["Auth0:M2M_Domain"] + "/api/v2/" + "\",\"grant_type\":\"client_credentials\"}", ParameterType.RequestBody);
        var response = await client.ExecuteAsync<Auth0ManagementResponse>(request);
        if (response == null || response.StatusCode != HttpStatusCode.OK || response.Data == null)
        {
            logger.LogCritical("Error in ManagementAPI token acquire!");
            return (DateTime.Now, false, string.Empty);
        }
        Auth0ManagementResponse resp = response.Data;
        if (resp.ExpiresIn == 0 || resp.TokenType == null || resp.AccessToken == null || resp.Scope == null)
        {
            logger.LogCritical("ManagementAPI Token is malformed! Check Auth0 configuration");
            return (DateTime.Now, false, string.Empty); //let's consider it "expired" if no "exp" claim is found (it should never happen)
        }

        //delete old token from db
        await tokenRepository.RemoveManagementApiTokenAsync();

        //put to db
        DateTime tokenExpireDateTime = DateTime.Now.AddSeconds(resp.ExpiresIn);
        await tokenRepository.AddManagementApiTokenAsync(resp.AccessToken, tokenExpireDateTime);
        logger.LogInformation("Auth0 Management API Token successfully saved");

        return (tokenExpireDateTime, true, resp.AccessToken);
    }
}
