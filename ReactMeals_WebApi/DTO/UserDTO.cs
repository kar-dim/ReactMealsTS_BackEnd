namespace ReactMeals_WebApi.DTO
{
    //used by Auth0UserDeserialize and Auth0UserSerialize
    public class UserIdentity
    {
        public string user_id { get; set; }
        public string provider { get; set; }
        public string connection { get; set; }
        public bool isSocial { get; set; }
    }

    //used by Auth0UserDeserialize and Auth0UserSerialize
    public class UserMetadata
    {
        public string name { get; set; }
        public string last_name { get; set; }
        public string address { get; set; }
    }

    //User data (in JSON) returned by the Auth0 management API, will be deserialized so that we will extract some of the data
    public class Auth0UserDeserialize
    {
        public DateTime created_at { get; set; }
        public string email { get; set; }
        public bool email_verified { get; set; }
        public List<UserIdentity> identities { get; set; }
        public string name { get; set; }
        public string nickname { get; set; }
        public string picture { get; set; }
        public DateTime updated_at { get; set; }
        public string user_id { get; set; }
        public UserMetadata user_metadata { get; set; }
        public DateTime last_login { get; set; }
        public string last_ip { get; set; }
        public int logins_count { get; set; }
    }

    //class that will be serialized and sent to the PATCH endpoint
    public class Auth0UserSerialize
    {
        public string email { get; set; }
        public UserMetadata user_metadata { get; set; }
    }
}
