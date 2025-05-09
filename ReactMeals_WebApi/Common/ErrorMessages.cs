namespace ReactMeals_WebApi.Common
{
    public static class ErrorMessages
    {
        public const string InternalError = "Internal server error. Please try again later.";
        public const string NotFound = "The requested resource was not found.";
        public const string Unauthorized = "You are not authorized to access this resource.";
        public const string Conflict = "A conflict occurred while processing the request.";
        public const string BadRequest = "The request could not be understood or was missing required parameters.";
        public const string BadDishPriceRequest = "Dish price is not valid";
        public const string BadDishNameRequest = "Dish name is not valid";
        public const string BadUpdateDishRequest = "Dish ID not found";
    }
}
