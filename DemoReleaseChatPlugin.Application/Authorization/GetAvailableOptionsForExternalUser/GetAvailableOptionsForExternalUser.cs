using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Authorization.GetAvailableOptionsForExternalUser;

public record GetAvailableOptionsForExternalUserQuery : IRequest<Result<GetAvailableOptionsForExternalUserResult>>
{
    public string ExternalId { get; set; }
}

public record GetAvailableOptionsForExternalUserResult
{
    public bool CanCreateUser { get; set; }
    public bool CanManageUser { get; set; }
    public Guid? UserId { get; set; }
}

public class GetAvailableOptionsForExternalUserValidator
    : AbstractValidator<GetAvailableOptionsForExternalUserQuery>
{
    public GetAvailableOptionsForExternalUserValidator() => RuleFor(x => x.ExternalId).NotEmpty();
}

/// <summary>
/// Returns a CAPABILITY OBJECT rather than the user or a 404, which keeps the page from having to
/// decide what "not found" means. Never both flags.
/// </summary>
public class GetAvailableOptionsForExternalUserHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetAvailableOptionsForExternalUserQuery, Result<GetAvailableOptionsForExternalUserResult>>
{
    public async ValueTask<Result<GetAvailableOptionsForExternalUserResult>> Handle(
        GetAvailableOptionsForExternalUserQuery request, CancellationToken cancellationToken)
    {
        var user = await readDb.Users
            .FirstOrDefaultAsync(x => x.ExternalId == request.ExternalId, cancellationToken);

        var result = user is null
            ? new GetAvailableOptionsForExternalUserResult { CanCreateUser = true }
            : new GetAvailableOptionsForExternalUserResult { CanManageUser = true, UserId = user.Id };

        return Result<GetAvailableOptionsForExternalUserResult>.OK(result);
    }
}
