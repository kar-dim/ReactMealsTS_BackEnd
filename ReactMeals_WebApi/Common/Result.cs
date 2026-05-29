namespace ReactMeals_WebApi.Common
{
    public record Result(bool IsSuccess, string Error)
    {
        public static Result Success() => new(true, null);
        public static Result Failure(string error) => new(false, error);
    }

    public record Result<T>(bool IsSuccess, string Error, T Value)
    {
        public static Result<T> Success(T value) => new(true, null, value);
        public static Result<T> Failure(string error) => new(false, error, default);
    }
}
