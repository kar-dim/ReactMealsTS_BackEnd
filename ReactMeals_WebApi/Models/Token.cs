namespace ReactMeals_WebApi.Models;

public class Token
{
    public Token() { }
    public Token(string tokenValue, string tokenType, DateTime expiryDate)
    {
        TokenValue = tokenValue;
        TokenType = tokenType;
        ExpiryDate = expiryDate;
    }
    public int TokenId { get; set; }
    //base64 encoded
    public string TokenValue { get; set; }
    //"M_API" used for now only (Auth0 Management API token)
    public string TokenType { get; set; }
    public DateTime ExpiryDate { get; set; }
}
