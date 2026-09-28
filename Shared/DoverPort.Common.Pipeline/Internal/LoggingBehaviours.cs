using System.Diagnostics;
using DoverPort.Common.Models.Exceptions;
using DoverPort.Common.Models.Users;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;

namespace DoverPort.Common.Pipeline.Internal;

/// All three of these no-op unless the matching LoggingSettings flag is switched on.

internal class RequestLoggerBehaviour<TRequest, TResponse>(
    LoggingSettings settings,
    IUserProvider userProvider,
    ILogger<RequestLoggerBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        if (!settings.EnableRequestLogger)
            return await next(message, cancellationToken);

        var user = await userProvider.ProvideAsync(cancellationToken);

        logger.LogInformation(
            "Incoming request '{RequestName}' from user '{User}' with params '{Params}'",
            typeof(TRequest).Name, user.DisplayName, message.ToSafeParams());

        return await next(message, cancellationToken);
    }
}

internal class RequestPerformanceBehaviour<TRequest, TResponse>(
    LoggingSettings settings,
    ILogger<RequestPerformanceBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        if (!settings.EnableRequestPerformanceLogger)
            return await next(message, cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        var response = await next(message, cancellationToken);

        stopwatch.Stop();

        if (settings.LongRequestIndicatorThreshold > 0 &&
            stopwatch.ElapsedMilliseconds > settings.LongRequestIndicatorThreshold)
        {
            logger.LogWarning(
                "Long running request '{RequestName}' took {ElapsedMilliseconds}ms, which is over the " +
                "{Threshold}ms threshold.",
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds, settings.LongRequestIndicatorThreshold);
        }

        return response;
    }
}

/// <summary>
/// Logs once and rethrows wrapped in <see cref="AlreadyLoggedException"/>, which is how the API
/// exception filter knows not to log the same failure a second time.
/// </summary>
internal class ExceptionsLoggerBehaviour<TRequest, TResponse>(
    LoggingSettings settings,
    ILogger<ExceptionsLoggerBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        if (!settings.EnableRichExceptionLogger)
            return await next(message, cancellationToken);

        try
        {
            return await next(message, cancellationToken);
        }
        catch (ValidationException exception)
        {
            logger.LogWarning(
                "Validation failed for request '{RequestName}': {Errors}",
                typeof(TRequest).Name,
                string.Join(" ", exception.Errors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}")));

            throw new AlreadyLoggedException(exception);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Request '{RequestName}' failed with params '{Params}'",
                typeof(TRequest).Name, message.ToSafeParams());

            throw new AlreadyLoggedException(exception);
        }
    }
}
