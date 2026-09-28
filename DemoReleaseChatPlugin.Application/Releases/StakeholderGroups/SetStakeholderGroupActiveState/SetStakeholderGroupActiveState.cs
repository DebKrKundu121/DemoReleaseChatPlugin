using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.SetStakeholderGroupActiveState;

public record SetStakeholderGroupActiveStateCommand : IRequest<Result>
{
    public StakeholderGroupId GroupId { get; set; }
    public bool IsActive { get; set; }
}

public class SetStakeholderGroupActiveStateValidator : AbstractValidator<SetStakeholderGroupActiveStateCommand>
{
    public SetStakeholderGroupActiveStateValidator() => RuleFor(x => x.GroupId).NotNull();
}

public class SetStakeholderGroupActiveStateHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<SetStakeholderGroupActiveStateCommand, Result>
{
    public async ValueTask<Result> Handle(
        SetStakeholderGroupActiveStateCommand request, CancellationToken cancellationToken)
    {
        var group = await dbContext.StakeholderGroups
            .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (group is null)
            return Result.Error("Stakeholder group not found.");

        var actor = await currentUser.ProvideAsync();

        var result = request.IsActive
            ? group.Reactivate(actor, clock)
            : group.Deactivate(actor, clock);

        if (result.IsFailure) return result;

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.OK();
    }
}
