package gr.jimmys.jimmysfoodzilla.common;

public record Result<T>(boolean isSuccess, String error, T value) {
    public static <T> Result<T> success() {
        return new Result<>(true, null, null);
    }

    public static <T> Result<T> success(T result) {
        return new Result<>(true, null, result);
    }

    public static <T> Result<T> failure(String error) {
        return new Result<>(false, error, null);
    }

    public T ResultValue() {
        return value;
    }
}