using System.Reflection;
using DoverPort.Common.Models;
using FluentValidation.Results;

namespace DoverPort.Common.Pipeline.Internal
{
    /// <summary>
    /// Builds a failed <see cref="Result"/>, <see cref="Result{T}"/> or <see cref="QueryResult{T}"/>
    /// for a request whose validators rejected it, so the pipeline can short-circuit by RETURNING a
    /// failure rather than throwing one.
    /// <para>
    /// A request that fails validation is an expected outcome - somebody left a field blank - so it
    /// is a value, not an exception. The only requests that still throw are the ones whose response
    /// type has no failure state to put the message in; for those there is nothing to return.
    /// </para>
    /// </summary>
    internal static class ValidationFailureResponse
    {
        /// <summary>
        /// Resolved once per closed response type and cached by the generic behaviour that calls it,
        /// so the reflection below runs at most once per request type in the process.
        /// </summary>
        internal static Func<string, object>? FactoryFor(Type responseType)
        {
            if (responseType == typeof(Result))
                return message => Result.Error(message);

            if (!responseType.IsGenericType)
                return null;

            var definition = responseType.GetGenericTypeDefinition();

            if (definition != typeof(Result<>) && definition != typeof(QueryResult<>))
                return null;

            // Result<T>.Error(string) and QueryResult<T>.Error(string) are both public statics
            // taking a single string, so the same lookup serves both.
            var error = responseType.GetMethod(
                "Error", BindingFlags.Public | BindingFlags.Static, null, [typeof(string)], null);

            return error is null ? null : message => error.Invoke(null, [message])!;
        }

        /// <summary>
        /// One line per distinct message. The presentation layer shows this as-is, so it has to read
        /// as a sentence a user can act on, not as a dump of rule names.
        /// </summary>
        internal static string Describe(IEnumerable<ValidationFailure> failures) =>
            string.Join(" ", failures.Select(x => x.ErrorMessage).Distinct());
    }
}
