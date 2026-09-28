namespace DoverPort.Common.Models
{
    /// <summary>
    /// The return type of a read that asks for one specific thing, where "it isn't there" is a
    /// legitimate answer rather than a failure.
    /// <para>
    /// <see cref="Result{T}"/> has two states and forces "not found" to be squeezed into the failure
    /// one, which loses information: the caller cannot tell a missing row from a broken read, so a
    /// page has no way to choose between an empty state and an error toast. This type has three,
    /// and leaves that choice to the presentation layer, which is the only layer that knows whether
    /// a missing stakeholder group means a 404, a blank panel or "no results".
    /// </para>
    /// <para>
    /// Reads that return a LIST do not use this - an empty list is already a complete answer.
    /// </para>
    /// </summary>
    public sealed class QueryResult<T>
    {
        public bool IsSuccess { get; }
        public bool IsNotFound { get; }
        public string Message { get; }
        public T? Value { get; }

        public bool IsFailure => !IsSuccess && !IsNotFound;

        private QueryResult(T? value, bool isSuccess, bool isNotFound, string message)
        {
            Value = value;
            IsSuccess = isSuccess;
            IsNotFound = isNotFound;
            Message = message;
        }

        public static QueryResult<T> Of(T value) => new(value, true, false, "OK");

        /// <summary>The read worked and the thing is genuinely not there.</summary>
        public static QueryResult<T> NotFound(string message = "Not found.") =>
            new(default, false, true, message);

        /// <summary>The read itself could not be completed.</summary>
        public static QueryResult<T> Error(string message) => new(default, false, false, message);

        /// <summary>
        /// Collapses down to a plain <see cref="Result{T}"/> for call sites that genuinely do not
        /// need to tell the two apart - the message is what they would have shown anyway.
        /// </summary>
        public Result<T> ToResult() =>
            IsSuccess ? Result<T>.OK(Value!) : Result<T>.Error(Message);
    }
}
