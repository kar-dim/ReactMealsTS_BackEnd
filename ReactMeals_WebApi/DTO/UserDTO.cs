using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.DTO;

//used by Auth0UserDeserialize and Auth0UserSerialize
public class UserIdentity
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("provider")]
    public string Provider { get; set; }

    [JsonPropertyName("connection")]
    public string Connection { get; set; }

    [JsonPropertyName("isSocial")]
    public bool IsSocial { get; set; }
}

//used by Auth0UserDeserialize and Auth0UserSerialize
public class UserMetadata
{
    public UserMetadata() { }
    public UserMetadata(string name, string lastName, string address)
    {
        Name = name;
        LastName = lastName;
        Address = address;
    }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("last_name")]
    public string LastName { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }
}

//User data (in JSON) returned by the Auth0 management API, will be deserialized so that we will extract some of the data
public class Auth0UserDeserialize
{
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("identities")]
    public List<UserIdentity> Identities { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }

    [JsonPropertyName("picture")]
    public string Picture { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("user_metadata")]
    public UserMetadata UserMetadata { get; set; }

    [JsonPropertyName("last_login")]
    public DateTime LastLogin { get; set; }

    [JsonPropertyName("last_ip")]
    public string LastIp { get; set; }

    [JsonPropertyName("logins_count")]
    public int LoginsCount { get; set; }
}

//class that will be serialized and sent to the PATCH endpoint
public class Auth0UserSerialize
{
    public Auth0UserSerialize() { }
    public Auth0UserSerialize(string email, UserMetadata userMetadata)
    {
        Email = email;
        UserMetadata = userMetadata;
    }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("user_metadata")]
    public UserMetadata UserMetadata { get; set; }
}
