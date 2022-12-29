using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ReactMeals_WebApi.Models;

namespace ReactMeals_WebApi.Services
{
    public class JimmysFoodzillaService
    {
        private readonly IMongoCollection<Dish> _dishesCollection;
        private readonly IMongoCollection<Order> _ordersCollection;

        public JimmysFoodzillaService(IOptions<JimmysFoodzillaDatabaseSettings> jimmysFoodzillaDatabaseSettings)
        {
            var mongoClient = new MongoClient(jimmysFoodzillaDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(jimmysFoodzillaDatabaseSettings.Value.DatabaseName);

            _dishesCollection = mongoDatabase.GetCollection<Dish>(jimmysFoodzillaDatabaseSettings.Value.DishesCollectionName);
            _ordersCollection = mongoDatabase.GetCollection<Order>(jimmysFoodzillaDatabaseSettings.Value.OrdersCollectionName);
        }

        public async Task<List<Dish>> GetDishesAsync() => await _dishesCollection.Find(_ => true).ToListAsync();
        public async Task<List<Order>> GetOrdersAsync() => await _ordersCollection.Find(_ => true).ToListAsync();

        public async Task<Dish?> GetDishAsync(long id) =>
        await _dishesCollection.Find(x => x.Dish_id == id).FirstOrDefaultAsync();

        public async Task<Order?> GetOrderAsync(string id) =>
        await _ordersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateDishAsync(Dish newDish) =>
            await _dishesCollection.InsertOneAsync(newDish);

        public async Task CreateOrderAsync(Order newOrder) =>
            await _ordersCollection.InsertOneAsync(newOrder);

        public async Task UpdateDishAsync(string id, Dish updatedDish) =>
            await _dishesCollection.ReplaceOneAsync(x => x.Id == id, updatedDish);
        public async Task UpdateOrderAsync(string id, Order updatedOrder) =>
            await _ordersCollection.ReplaceOneAsync(x => x.Id == id, updatedOrder);

        public async Task RemoveDishAsync(long id) =>
            await _dishesCollection.DeleteOneAsync(x => x.Dish_id == id);
        public async Task RemoveOrderAsync(string id) =>
            await _ordersCollection.DeleteOneAsync(x => x.Id == id);
    }
}
