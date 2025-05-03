namespace ReactMeals_WebApi.Common
{
    public record Result(bool IsSuccess, string Error, object ResultValue)
    {
        public static Result Success() => new(true, null, null);
        public static Result Success(object result) => new(true, null, result);
        public static Result Failure(string error) => new(false, error, null);
    }
}
