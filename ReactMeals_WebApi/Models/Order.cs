﻿using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;

namespace ReactMeals_WebApi.Models
{
    public class OrderItem
    {
        public int Dish_id { get; set; }
        public int Dish_counter { get; set; }
    }
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [IgnoreDataMember]
        public string? Id { get; set; }
        public OrderItem[]? order { get; set; }   
    }
}
