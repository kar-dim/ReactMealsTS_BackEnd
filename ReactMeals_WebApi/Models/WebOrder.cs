using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace ReactMeals_WebApi.Models
{
    public class WebOrderItem
    {
        public int? Id { get; set; }
        //foreign key to Dish
        public int DishId { get; set; }
        public Dish Dish { get; set; } //used by EF
        //foreign key to Order (auto-discovered by EF because of <Name> + <Id>)
        public int OrderId { get; set; }
        public WebOrder Order { get; set; } //used by EF
        public int Dish_counter { get; set; }
    }
    public class WebOrder
    {
        public int Id { get; set; }

        [Precision(18, 2)]
        [JsonPropertyName("totalCost")]
        public decimal TotalCost {  get; set; }
        [JsonPropertyName("order")]
        public ICollection<WebOrderItem> Order { get; set; }  
        public User User { get; set; } //used by EF
        public string UserId { get; set; } //foreign key to User (auto-discovered by EF because of <Name> + <Id>)
    }
}
