using DoverPort.Common.Models.Users;
using Newtonsoft.Json;

namespace DoverPort.Common.Pipeline.Internal;

internal static class Extensions
{
    internal static string ToSafeParams<TRequest>(this TRequest message)
    {
        try
        {
            var safeParams = JsonConvert.SerializeObject(message);
            return safeParams.Length > 1000 ? safeParams[..1000] + "..." : safeParams;
        }
        catch
        {
            return "[Unserializable]";
        }
    }
}

internal record LoggingSettings
{
    public required bool EnableRequestLogger { get; init; }
    public required bool EnableRequestPerformanceLogger { get; init; }
    public required bool EnableRichExceptionLogger { get; init; }
    public required int LongRequestIndicatorThreshold { get; init; }
}

internal class PipelineUserProvider : IUserProvider
{
    public async Task<CurrentUser> ProvideAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return new AnonymousUser();
    }
}
