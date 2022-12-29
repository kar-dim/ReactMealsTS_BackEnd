using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.Models
{
    public class Dish
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonIgnore]
        public string? Id { get; set; }

        public long Dish_id { get; set; }

        public string? Dish_name { get; set; }
        public string? Dish_description { get; set; }
        public double Price { get; set; }
    }
}
