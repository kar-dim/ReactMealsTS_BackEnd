package gr.jimmys.jimmysfoodzilla.common;

public record Result(boolean isSuccess, String error, Object ResultValue) {
    public static Result success() {
        return new Result(true, null, null);
    }

    public static Result success(Object result) {
        return new Result(true, null, result);
    }

    public static Result failure(String error) {
        return new Result(false, error, null);
    }
}