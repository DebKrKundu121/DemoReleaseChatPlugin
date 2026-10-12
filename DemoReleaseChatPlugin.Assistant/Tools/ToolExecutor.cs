using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Assistant.Tools;

/// The shield every data tool's body runs inside. A failed lookup becomes an { error } payload the
/// model can explain to the user, rather than an exception that kills the whole turn.
internal static class ToolExecutor
{
    public static async Task<object> RunAsync(Func<Task<object>> action)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            throw;                                  // a cancelled turn must actually stop
        }
        catch (Exception)
        {
            // Details withheld: the model (and so the user) gets a sentence, never a stack trace or
            // a connection string.
            return Error("The lookup failed unexpectedly. Tell the user the data could not be retrieved.");
        }
    }

    /// Validation failures arrive here too: this app's Mediator pipeline returns a failed Result
    /// rather than throwing, so its message tells the model how to correct its arguments.
    public static object Error(string message) => new { error = message };

    public static object Error<T>(Result<T> result) => Error(result.Message);

    /// The same for the queries that return QueryResult, where "not found" is its own outcome — its
    /// message (e.g. "No application named 'X'…") tells the model what to try next.
    public static object Error<T>(QueryResult<T> result) => Error(result.Message);
}
