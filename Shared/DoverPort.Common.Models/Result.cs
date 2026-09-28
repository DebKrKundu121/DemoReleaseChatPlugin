namespace DoverPort.Common.Models
{
    /// <summary>
    /// The return type of every command handler that carries no value.
    /// Handlers do not throw for expected failures - they return a failed Result and let the call
    /// site read <see cref="Message"/>. Exceptions are reserved for bugs.
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public bool IsFailure => !IsSuccess;

        protected Result(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static Result OK() => new(true, "OK");
        public static Result Error() => new(false, "An unexpected error has occurred. Please try again later.");
        public static Result Error(string message) => new(false, message);
    }

    /// <summary>
    /// The return type of every handler that carries a value.
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public bool IsFailure => !IsSuccess;
        public bool HasValue { get; }
        public bool HasNoValue { get; }
        public T? Value { get; }

        protected Result(T? value, bool isSuccess, string message)
        {
            Value = value;
            HasValue = Value != null;
            HasNoValue = Value == null;
            IsSuccess = isSuccess;
            Message = message;
        }

        public static Result<T> OK(T value) => new(value, true, "OK");
        public static Result<T> Error() => new(default, false, "An unexpected error has occurred. Please try again later.");
        public static Result<T> Error(string message) => new(default, false, message);

        public static implicit operator Result<T>(T value)
        {
            if (value?.GetType() == typeof(Result<T>))
                return (Result<T>)(object)value;

            return OK(value);
        }
    }
}
