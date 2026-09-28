using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Authorization.UpdateUserRoles;

public record UpdateUserRolesCommand : IRequest<Result>
{
    public Guid UserId { get; set; }
    public List<RoleSelection> Roles { get; set; } = [];

    public record RoleSelection
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsAssigned { get; set; }
    }
}

public record GetUpdateUserRolesQuery : IRequest<QueryResult<GetUpdateUserRolesResult>>
{
    public Guid UserId { get; set; }
}

public record GetUpdateUserRolesResult
{
    public UpdateUserRolesCommand Command { get; set; }
}

public class UpdateUserRolesValidator : AbstractValidator<UpdateUserRolesCommand>
{
    public UpdateUserRolesValidator() => RuleFor(x => x.UserId).NotEmpty();
}

public class GetUpdateUserRolesValidator : AbstractValidator<GetUpdateUserRolesQuery>
{
    public GetUpdateUserRolesValidator() => RuleFor(x => x.UserId).NotEmpty();
}

/// Returns EVERY role with IsAssigned computed per user, so the UI can render switches.
public class GetUpdateUserRolesHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetUpdateUserRolesQuery, QueryResult<GetUpdateUserRolesResult>>
{
    public async ValueTask<QueryResult<GetUpdateUserRolesResult>> Handle(
        GetUpdateUserRolesQuery request, CancellationToken cancellationToken)
    {
        var userExists = await readDb.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken);

        if (!userExists)
            return QueryResult<GetUpdateUserRolesResult>.NotFound("User not found.");

        var roles = await readDb.Roles
            .OrderBy(x => x.Name)
            .Select(x => new UpdateUserRolesCommand.RoleSelection
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsAssigned = x.Users.Any(u => u.Id == request.UserId)
            })
            .ToListAsync(cancellationToken);

        return QueryResult<GetUpdateUserRolesResult>.Of(new GetUpdateUserRolesResult
        {
            Command = new UpdateUserRolesCommand { UserId = request.UserId, Roles = roles }
        });
    }
}

/// <summary>
/// Set-and-replace rather than add/remove deltas, so the command is idempotent and there is no
/// partial-update state to reason about. The trade-off - a concurrent edit silently wins - is
/// acceptable for a page two administrators will never use simultaneously.
/// </summary>
public class UpdateUserRolesHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUserRolesCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        // The .Include is REQUIRED. Without it EF has no original collection to diff against and the
        // existing join rows are left behind.
        var user = await dbContext.Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.Error("User not found.");

        var assignedRoleIds = request.Roles.Where(x => x.IsAssigned).Select(x => x.Id).ToList();

        var roles = await dbContext.Roles
            .Where(x => assignedRoleIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        user.Roles = roles;   // EF diffs the join table

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.OK();
    }
}
