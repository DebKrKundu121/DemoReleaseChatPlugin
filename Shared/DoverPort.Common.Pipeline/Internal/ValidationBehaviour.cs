using DoverPort.Common.Models.Users;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DoverPort.Common.Pipeline.Internal;

/// <summary>
/// Runs every registered validator for a request before the handler is reached, and SHORT-CIRCUITS
/// WITH A FAILED RESULT when one rejects it.
/// <para>
/// This used to throw <see cref="ValidationException"/>. A blank required field is a normal thing
/// for a user to do, not a bug, so it travels back as a value on the response the caller was
/// already going to inspect - the same <c>Result</c> / <c>Result&lt;T&gt;</c> / <c>QueryResult&lt;T&gt;</c>
/// every handler returns. Call sites that already read <c>.IsFailure</c> and <c>.Message</c> need no
/// change, and the ones that wrapped <c>Mediator.Send</c> in try/catch to cope with the throw no
/// longer have to.
/// </para>
/// <para>
/// A request whose response type has no failure state to carry the message - anything that is not
/// one of those three - still throws, because there is genuinely nothing to return. There are none
/// in this application today; the branch exists so that adding one fails loudly rather than
/// silently dropping the validation result.
/// </para>
/// <para>
/// One thing that has NOT changed: validators resolve from a FRESH scope, so a validator injecting
/// the read database gets a different instance from the handler's. Fine for the read-only rules
/// they contain.
/// </para>
/// </summary>
internal class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// Resolved once per closed TResponse, which is once per request type in the process.
    private static readonly Func<string, object>? FailureFactory =
        ValidationFailureResponse.FactoryFor(typeof(TResponse));

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationBehaviour<TRequest, TResponse>> _logger;

    public ValidationBehaviour(IServiceProvider serviceProvider,
        ILogger<ValidationBehaviour<TRequest, TResponse>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var validators = scope.ServiceProvider.GetRequiredService<IEnumerable<IValidator<TRequest>>>().ToList();

        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(message);

            var validationResults = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
            {
                var userProvider = _serviceProvider.GetRequiredService<IUserProvider>();
                var user = await userProvider.ProvideAsync(cancellationToken);

                // Information, not Warning or Error: an expected business outcome is not a fault.
                _logger.LogInformation(
                    "Validation rejected a request. Request: {RequestName}, User: {User}, Params: {Params}",
                    typeof(TRequest).Name, user.DisplayName, message.ToSafeParams());

                var description = ValidationFailureResponse.Describe(failures);

                if (FailureFactory is not null)
                    return (TResponse)FailureFactory(description);

                // No failure state on TResponse to carry the message - nothing to return.
                throw new ValidationException(failures);
            }
        }

        return await next(message, cancellationToken);
    }
}
