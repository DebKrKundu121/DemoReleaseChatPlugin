using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DoverPort.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Authorization.GetRolesByExternalId;

public record GetRolesByExternalIdQuery : IRequest<Result<IReadOnlyList<string>>>
{
    public string ExternalId { get; set; }
}

/// <summary>
/// NO VALIDATOR on this one, deliberately. It runs on the auth path, where a thrown
/// ValidationException would break sign-in rather than produce a message. The handler is safe with
/// any input, returning an empty list for an unknown id.
/// </summary>
public class GetRolesByExternalIdHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetRolesByExternalIdQuery, Result<IReadOnlyList<string>>>
{
    public async ValueTask<Result<IReadOnlyList<string>>> Handle(
        GetRolesByExternalIdQuery request, CancellationToken cancellationToken)
    {
        var roles = await readDb.Roles
            .Where(x => x.Users.Any(u => u.ExternalId == request.ExternalId))
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<string>>.OK(roles);
    }
}
