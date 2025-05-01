using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.DTO
{
    public record ManagementResponseDTO(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("token_type")] string TokenType);
}
