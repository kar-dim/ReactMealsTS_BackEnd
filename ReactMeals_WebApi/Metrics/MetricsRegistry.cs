using App.Metrics;
using App.Metrics.Counter;

namespace ReactMeals_WebApi.Metrics
{
    public class MetricsRegistry
    {
        public static CounterOptions CreatedOrdersCounter => new CounterOptions
        {
            Name = "Created Orders",
            Context = "ReactMeals_WebApi",
            MeasurementUnit = Unit.Calls
        };

        public static CounterOptions DishRequestCounter => new CounterOptions
        {
            Name = "(One) Dish Request",
            Context = "ReactMeals_WebApi",
            MeasurementUnit = Unit.Calls
        };

        public static CounterOptions DishesRequestCounter => new CounterOptions
        {
            Name = "Get All Dishes Request",
            Context = "ReactMeals_WebApi",
            MeasurementUnit = Unit.Calls
        };
    }
}
