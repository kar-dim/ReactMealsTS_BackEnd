using ReactMeals_WebApi.Common;
using ReactMeals_WebApi.DTO;
using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Services.Interfaces;
using RestSharp;
using System.Net;

namespace ReactMeals_WebApi.Services.Implementations;

public class JwtService(ILogger<JwtService> logger, IConfiguration configuration, RestClient client) : IJwtService
{
    private readonly ManagementInputDTO requestBody = new ManagementInputDTO(
        ClientId: configuration["Auth0:M2M_ClientID"],
        ClientSecret: configuration["Auth0:M2M_ClientSecret"],
        Audience: $"https://{configuration["Auth0:M2M_Domain"]}/api/v2/",
        GrantType: "client_credentials"
     );
    
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
        if (tokenData.ExpiresIn <= 30 || !string.Equals(tokenData.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(tokenData.AccessToken) || tokenData.Scope == null)
        {
            logger.LogCritical("ManagementAPI Token is malformed! Check Auth0 configuration");
            return null; //let's consider it "expired" if no "exp" claim is found (it should never happen)
        }

        DateTime tokenExpireDateTime = DateTime.Now.AddSeconds(tokenData.ExpiresIn);
        return new Token(tokenData.AccessToken, TokenType.MANAGEMENT_API, tokenExpireDateTime);
    }
}
