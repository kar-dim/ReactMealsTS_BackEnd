using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services.Interfaces;
using RestSharp;
using System.Net;

namespace ReactMeals_WebApi.Services.Implementations;

public class JwtService(IServiceScopeFactory serviceScopeFactory, ILogger<JwtService> logger, IConfiguration configuration, RestClient client) : IJwtService
{
    private readonly TokenRepository tokenRepository = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<TokenRepository>();
    private readonly ManagementInputDTO requestBody = new ManagementInputDTO(
        ClientId: configuration["Auth0:M2M_ClientID"],
        ClientSecret: File.ReadAllText("m2m_secret.txt").Trim(),
        Audience: $"https://{configuration["Auth0:M2M_Domain"]}/api/v2/",
        GrantType: "client_credentials"
     );
    
    public async Task<Token> RetrieveToken()
    {
        var tokenFromDb = await tokenRepository.GetManagementApiTokenAsync();
        if (tokenFromDb == null)
            logger.LogInformation("No ManagementAPI Token found in db...");
        return tokenFromDb;
    }

    //call the Auth0 Rest service to renew the token
    public async Task<Token> RenewToken()
    {
        var request = new RestRequest("oauth/token", Method.Post).AddJsonBody(requestBody);
        var response = await client.ExecuteAsync<ManagementResponseDTO>(request);
        if (response == null || response.StatusCode != HttpStatusCode.OK || response.Data == null)
        {
            logger.LogCritical("Error in ManagementAPI token acquire!");
            return null;
        }
        var tokenData = response.Data;
        if (tokenData.ExpiresIn == 0 || tokenData.TokenType == null || tokenData.AccessToken == null || tokenData.Scope == null)
        {
            logger.LogCritical("ManagementAPI Token is malformed! Check Auth0 configuration");
            return null; //let's consider it "expired" if no "exp" claim is found (it should never happen)
        }

        //delete old token from db
        await tokenRepository.RemoveManagementApiTokenAsync();

        //put to db
        DateTime tokenExpireDateTime = DateTime.Now.AddSeconds(tokenData.ExpiresIn);
        await tokenRepository.AddManagementApiTokenAsync(tokenData.AccessToken, tokenExpireDateTime);
        logger.LogInformation("Auth0 Management API Token successfully saved");

        return new Token(tokenData.AccessToken, TokenType.MANAGEMENT_API, tokenExpireDateTime);
    }
}
