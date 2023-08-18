using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.Services
{
    public class Auth0ManagementResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
    public class JwtService
    {
        private readonly MainDbContext _mainDbContext;
        private readonly ILogger<JwtService> _logger;
        private readonly IConfiguration _configuration;
        private static DateTime originDateTime = new DateTime(1980, 1, 1, 0, 0, 0, 0);
        public JwtService(MainDbContext mainDbContext, ILogger<JwtService> logger, IConfiguration congiguration)
        {
            _mainDbContext = mainDbContext;
            _logger = logger;
            _configuration = congiguration;
        }
        public async Task<(bool, DateTime?)> IsTokenExpired()
        {
            //check if the current token is expired
            //Return true if expired, false otherwise
            var tokenFromDb = await _mainDbContext.Tokens.Where(x => x.TokenType.Equals("M_API")).FirstOrDefaultAsync();
            if (tokenFromDb == null)
            {
                _logger.LogInformation("JwtService: No ManagementAPI Token found in db, fetching new...");
                return (true, null); //no token found
            }
            
            return (tokenFromDb.ExpiryDate <= DateTime.Now, tokenFromDb.ExpiryDate);
        }

        public async Task<(DateTime,bool)> RenewToken()
        {
            var client = new RestClient("https://" + _configuration["Auth0:M2M_Domain"]);
            var request = new RestRequest("oauth/token", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/x-www-form-urlencoded", "{\"client_id\":\"" + _configuration["Auth0:M2M_ClientID"] + "\",\"client_secret\":\"" + _configuration["Auth0:M2M_ClientSecret"] + "\",\"audience\":\"" + "https://" + _configuration["Auth0:M2M_Domain"] + "/api/v2/" + "\",\"grant_type\":\"client_credentials\"}", ParameterType.RequestBody);
            var response = await client.ExecuteAsync<Auth0ManagementResponse>(request);
            if (response == null || response.StatusCode != HttpStatusCode.OK || response.Data ==  null)
            {
                _logger.LogCritical("JwtSercice: Error in ManagementAPI token acquire!");
                return (DateTime.Now, false);
            }
            Auth0ManagementResponse resp = response.Data;
            if (resp.ExpiresIn == null || resp.TokenType == null || resp.AccessToken == null || resp.Scope == null)
            {
                _logger.LogCritical("JwtService: ManagementAPI Token is malformed! Check Auth0 configuration");
                return (DateTime.Now, false); //let's consider it "expired" if no "exp" claim is found (it should never happen)
            }
            
            Token? tokenFromDb = await _mainDbContext.Tokens.Where(x => x.TokenType == "M_API").FirstOrDefaultAsync();
            if (tokenFromDb != null) {
                _mainDbContext.Tokens.Remove(tokenFromDb);
            }

            DateTime tokenExpireDateTime = DateTime.Now.AddSeconds(resp.ExpiresIn.Value);
            Token newToken = new Token
            {
                TokenValue = resp.AccessToken,
                TokenType = "M_API",
                ExpiryDate = tokenExpireDateTime
            };

            //put to db
            await _mainDbContext.Tokens.AddAsync(newToken);
            await _mainDbContext.SaveChangesAsync();

            _logger.LogInformation("JwtService: Auth0 Management API Token successfully saved");
            return (tokenExpireDateTime, true);
        }
    }
}
