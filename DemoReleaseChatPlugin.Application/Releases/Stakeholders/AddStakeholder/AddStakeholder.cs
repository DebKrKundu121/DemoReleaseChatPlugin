using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Stakeholders.AddStakeholder;

public record AddStakeholderCommand : IRequest<Result<StakeholderId>>
{
    public StakeholderGroupId GroupId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class AddStakeholderValidator : AbstractValidator<AddStakeholderCommand>
{
    private readonly IDemoReleaseChatPluginReadDb _readDb;

    public AddStakeholderValidator(IDemoReleaseChatPluginReadDb readDb)
    {
        _readDb = readDb;

        RuleFor(x => x.GroupId).NotNull();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        // The format check is EmailAddress.Of's job now; this rule only reports it in the field.
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(EmailAddress.MaxLength)
            .Must(BeAWellFormedAddress).WithMessage("Please give a valid email address.")
            .MustAsync(BeUniqueInScope)
            .WithMessage("This email is already a stakeholder here. A person can only belong to one group.");
    }

    private static bool BeAWellFormedAddress(string email) => EmailAddress.Of(email).IsSuccess;

    /// Resolves scope from the GROUP, so the uniqueness rule is "one email per project scope".
    /// Returns true when the group cannot be found, leaving that case to the handler's Result.Error.
    private async Task<bool> BeUniqueInScope(
        AddStakeholderCommand command, string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;

        var group = await _readDb.StakeholderGroups
            .FirstOrDefaultAsync(x => x.Id == command.GroupId, cancellationToken);

        if (group is null) return true;

        var address = EmailAddress.Of(email);

        if (address.IsFailure) return true;

        return !await _readDb.Stakeholders
            .AnyAsync(x => x.ProjectId == group.ProjectId && x.Email == address.Value, cancellationToken);
    }
}

public class AddStakeholderHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<AddStakeholderCommand, Result<StakeholderId>>
{
    public async ValueTask<Result<StakeholderId>> Handle(
        AddStakeholderCommand request, CancellationToken cancellationToken)
    {
        // The .Include is required: the new child is discovered through the tracked aggregate, and
        // there is deliberately no dbContext.Stakeholders.Add(...) - going through the aggregate is
        // the only way to construct a Stakeholder.
        var group = await dbContext.StakeholderGroups
            .Include(x => x.Stakeholders)
            .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (group is null)
            return Result<StakeholderId>.Error("Stakeholder group not found.");

        var email = EmailAddress.Of(request.Email);
        if (email.IsFailure) return Result<StakeholderId>.Error(email.Message);

        var actor = await currentUser.ProvideAsync();

        var stakeholder = group.AddStakeholder(StakeholderId.New(), request.Name, email.Value, actor, clock);
        if (stakeholder.IsFailure) return Result<StakeholderId>.Error(stakeholder.Message);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result<StakeholderId>.OK(stakeholder.Value.Id);
    }
}
