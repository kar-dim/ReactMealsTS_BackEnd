using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.Models;

public class User
{
    public User() { }

    public User(string user_id, string email, string name, string lastName, string address)
    {
        User_Id = user_id;
        Email = email;
        Name = name;
        LastName = lastName;
        Address = address;
    }

    public void UpdateUser(User other)
    {
        User_Id = other.User_Id;
        Email = other.Email;
        Name = other.Name;
        LastName = other.LastName;
        Address = other.Address;
    }

    [Key]
    [JsonPropertyName("user_id")]
    public string User_Id { get; set; } //"user_id" from auth0

    public string Email { get; set; } //"email" from auth0

    //data from custom auth0 user_metadata
    public string Name { get; set; } //"name"
    public string LastName { get; set; } //"last_name"
    public string Address { get; set; } //"address"

    public override string ToString()
    {
        return "User ID " + User_Id + " email: " + Email + " Name: " + Name + " Last Name: " + LastName + " Address: " + Address + "\n";
    }
}
