using System.ComponentModel.DataAnnotations;

namespace ReactMeals_WebApi.Models
{
    public class User
    {
        [Key]
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
}
