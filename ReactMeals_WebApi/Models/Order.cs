using Microsoft.EntityFrameworkCore;

namespace ReactMeals_WebApi.Models
{
    public class OrderItem
    {
        public int? Id { get; set; }
        //foreign key to Dish
        public int DishId { get; set; }
        public Dish Dish { get; set; } //used by EF
        //foreign key to Order (auto-discovered by EF because of <Name> + <Id>)
        public int OrderId { get; set; }
        public Order Order { get; set; } //used by EF
        public int Dish_counter { get; set; }
    }
    public class Order
    {
        public int? Id { get; set; }

        [Precision(18, 2)]
        public decimal? totalCost {  get; set; }
        public ICollection<OrderItem>? order { get; set; }  
        public User User { get; set; } //used by EF
        public string UserId { get; set; } //foreign key to User (auto-discovered by EF because of <Name> + <Id>)
    }
}
