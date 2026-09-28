using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DoverPort.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Authorization.GetUsersWithRoles;

public record GetUsersWithRolesQuery : IRequest<Result<GetUsersWithRolesResult>>;

public record GetUsersWithRolesResult
{
    public IReadOnlyList<UserDto> Users { get; set; } = [];

    public record UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ExternalId { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = [];
    }
}

/// One of two symmetric read models for the two tabs. Orders by Name at every level and projects
/// inside the query.
public class GetUsersWithRolesHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetUsersWithRolesQuery, Result<GetUsersWithRolesResult>>
{
    public async ValueTask<Result<GetUsersWithRolesResult>> Handle(
        GetUsersWithRolesQuery request, CancellationToken cancellationToken)
    {
        var users = await readDb.Users
            .OrderBy(x => x.Name)
            .Select(x => new GetUsersWithRolesResult.UserDto
            {
                Id = x.Id,
                Name = x.Name,
                ExternalId = x.ExternalId,
                Roles = x.Roles.OrderBy(r => r.Name).Select(r => r.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return Result<GetUsersWithRolesResult>.OK(new GetUsersWithRolesResult { Users = users });
    }
}
