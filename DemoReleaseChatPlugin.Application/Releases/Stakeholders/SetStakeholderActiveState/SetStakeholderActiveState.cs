using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Stakeholders.SetStakeholderActiveState;

public record SetStakeholderActiveStateCommand : IRequest<Result>
{
    public StakeholderId StakeholderId { get; set; }
    public bool IsActive { get; set; }
}

public class SetStakeholderActiveStateValidator : AbstractValidator<SetStakeholderActiveStateCommand>
{
    public SetStakeholderActiveStateValidator() => RuleFor(x => x.StakeholderId).NotNull();
}

public class SetStakeholderActiveStateHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<SetStakeholderActiveStateCommand, Result>
{
    public async ValueTask<Result> Handle(
        SetStakeholderActiveStateCommand request, CancellationToken cancellationToken)
    {
        var stakeholder = await dbContext.Stakeholders
            .FirstOrDefaultAsync(x => x.Id == request.StakeholderId, cancellationToken);

        if (stakeholder is null)
            return Result.Error("Stakeholder not found.");

        var actor = await currentUser.ProvideAsync();

        var result = request.IsActive
            ? stakeholder.Reactivate(actor, clock)
            : stakeholder.Deactivate(actor, clock);

        if (result.IsFailure) return result;

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.OK();
    }
}
