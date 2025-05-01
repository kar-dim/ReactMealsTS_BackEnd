using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.DTO
{
    public record ManagementInputDTO(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("client_secret")] string ClientSecret,
    [property: JsonPropertyName("audience")] string Audience,
    [property: JsonPropertyName("grant_type")] string GrantType);
}
