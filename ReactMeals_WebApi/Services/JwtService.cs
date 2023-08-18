using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Models;
using RestSharp;
using System.Net;
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
        private string _className;
        private readonly MainDbContext _mainDbContext;
        private readonly ILogger<JwtService> _logger;
        private readonly IConfiguration _configuration;
        public JwtService(MainDbContext mainDbContext, ILogger<JwtService> logger, IConfiguration congiguration)
        {
            _className = nameof(JwtService) + ": ";
            _mainDbContext = mainDbContext;
            _logger = logger;
            _configuration = congiguration;
        }
        public async Task<(bool, DateTime?, string)> IsTokenExpired()
        {
            //check if the current token is expired
            //Return true if expired, false otherwise
            var tokenFromDb = await _mainDbContext.Tokens.Where(x => x.TokenType.Equals("M_API")).FirstOrDefaultAsync();
            if (tokenFromDb == null)
            {
                _logger.LogInformation(_className + "No ManagementAPI Token found in db, fetching new...");
                return (true, null, string.Empty); //no token found
            }
            
            return (tokenFromDb.ExpiryDate <= DateTime.Now, tokenFromDb.ExpiryDate, tokenFromDb.TokenValue);
        }

        public async Task<(DateTime,bool, string)> RenewToken()
        {
            RestClient client = new RestClient("https://" + _configuration["Auth0:M2M_Domain"]);
            RestRequest request = new RestRequest("oauth/token", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/x-www-form-urlencoded", "{\"client_id\":\"" + _configuration["Auth0:M2M_ClientID"] + "\",\"client_secret\":\"" + _configuration["Auth0:M2M_ClientSecret"] + "\",\"audience\":\"" + "https://" + _configuration["Auth0:M2M_Domain"] + "/api/v2/" + "\",\"grant_type\":\"client_credentials\"}", ParameterType.RequestBody);
            var response = await client.ExecuteAsync<Auth0ManagementResponse>(request);
            if (response == null || response.StatusCode != HttpStatusCode.OK || response.Data ==  null)
            {
                _logger.LogCritical(_className + "Error in ManagementAPI token acquire!");
                return (DateTime.Now, false, string.Empty);
            }
            Auth0ManagementResponse resp = response.Data;
            if (resp.ExpiresIn == null || resp.TokenType == null || resp.AccessToken == null || resp.Scope == null)
            {
                _logger.LogCritical(_className + "ManagementAPI Token is malformed! Check Auth0 configuration");
                return (DateTime.Now, false, string.Empty); //let's consider it "expired" if no "exp" claim is found (it should never happen)
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

            _logger.LogInformation(_className + "Auth0 Management API Token successfully saved");
            return (tokenExpireDateTime, true, resp.AccessToken);
        }
    }
}
