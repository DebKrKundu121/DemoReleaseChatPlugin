using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DoverPort.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Authorization.GetRolesWithUsers;

public record GetRolesWithUsersQuery : IRequest<Result<GetRolesWithUsersResult>>;

public record GetRolesWithUsersResult
{
    public IReadOnlyList<RoleDto> Roles { get; set; } = [];

    public record RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IReadOnlyList<UserDto> Users { get; set; } = [];
    }

    public record UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}

public class GetRolesWithUsersHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetRolesWithUsersQuery, Result<GetRolesWithUsersResult>>
{
    public async ValueTask<Result<GetRolesWithUsersResult>> Handle(
        GetRolesWithUsersQuery request, CancellationToken cancellationToken)
    {
        var roles = await readDb.Roles
            .OrderBy(x => x.Name)
            .Select(x => new GetRolesWithUsersResult.RoleDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Users = x.Users
                    .OrderBy(u => u.Name)
                    .Select(u => new GetRolesWithUsersResult.UserDto { Id = u.Id, Name = u.Name })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Result<GetRolesWithUsersResult>.OK(new GetRolesWithUsersResult { Roles = roles });
    }
}
