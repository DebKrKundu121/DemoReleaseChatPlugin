using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Projects.SetProjectArchiveState;

public record SetProjectArchiveStateCommand : IRequest<Result>
{
    public ProjectId ProjectId { get; set; }
    public bool IsArchived { get; set; }
}

public class SetProjectArchiveStateValidator : AbstractValidator<SetProjectArchiveStateCommand>
{
    public SetProjectArchiveStateValidator() => RuleFor(x => x.ProjectId).NotNull();
}

/// <summary>
/// "Already archived" and "not archived" are decided by <c>Project</c>, not here - the handler
/// picks the method and relays whatever the aggregate says.
/// </summary>
public class SetProjectArchiveStateHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<SetProjectArchiveStateCommand, Result>
{
    public async ValueTask<Result> Handle(
        SetProjectArchiveStateCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(x => x.Id == request.ProjectId, cancellationToken);

        if (project is null)
            return Result.Error("Project not found.");

        var actor = await currentUser.ProvideAsync();

        var result = request.IsArchived
            ? project.Archive(actor, clock)
            : project.Restore(actor, clock);

        if (result.IsFailure) return result;

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.OK();
    }
}
