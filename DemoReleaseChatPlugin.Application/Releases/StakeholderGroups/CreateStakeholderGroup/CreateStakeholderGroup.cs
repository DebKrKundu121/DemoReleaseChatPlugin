using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.CreateStakeholderGroup;

public class NewStakeholderInput
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public record CreateStakeholderGroupCommand : IRequest<Result<StakeholderGroupId>>
{
    /// Null for a mandatory (organisation-wide) group.
    public ProjectId? ProjectId { get; set; }

    public bool IsMandatory { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<NewStakeholderInput> Stakeholders { get; set; } = [];
}

public class NewStakeholderInputValidator : AbstractValidator<NewStakeholderInput>
{
    public NewStakeholderInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(EmailAddress.MaxLength)
            .Must(email => EmailAddress.Of(email).IsSuccess)
            .WithMessage("Please give a valid email address.");
    }
}

public class CreateStakeholderGroupValidator : AbstractValidator<CreateStakeholderGroupCommand>
{
    private readonly IDemoReleaseChatPluginReadDb _readDb;

    public CreateStakeholderGroupValidator(IDemoReleaseChatPluginReadDb readDb)
    {
        _readDb = readDb;

        RuleFor(x => x.ProjectId).NotNull().When(x => !x.IsMandatory)
            .WithMessage("A project is required for a project stakeholder group.");

        RuleFor(x => x.ProjectId).Null().When(x => x.IsMandatory)
            .WithMessage("A mandatory stakeholder group is not tied to a project.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).MustAsync(BeUniqueName)
            .WithMessage("A stakeholder group with this name already exists here.");

        RuleFor(x => x.Description).MaximumLength(500);

        RuleForEach(x => x.Stakeholders).SetValidator(new NewStakeholderInputValidator());

        RuleFor(x => x.Stakeholders)
            .Must(HaveDistinctEmails)
                .WithMessage("The same email is listed twice in this group.")
            .MustAsync(BeFreeOfExistingEmails)
                .WithMessage("One of these emails is already a stakeholder here. A person can only belong to one group.");
    }

    /// Scope is THIS PROJECT, or ALL MANDATORY GROUPS when ProjectId is null.
    private async Task<bool> BeUniqueName(
        CreateStakeholderGroupCommand command, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;

        var candidate = name.Trim();

        return command.ProjectId is not null
            ? !await _readDb.StakeholderGroups
                .AnyAsync(x => x.ProjectId == command.ProjectId && x.Name == candidate, cancellationToken)
            : !await _readDb.StakeholderGroups
                .AnyAsync(x => x.ProjectId == null && x.Name == candidate, cancellationToken);
    }

    private static bool HaveDistinctEmails(List<NewStakeholderInput> stakeholders)
    {
        if (stakeholders is null) return true;

        var emails = ValidAddresses(stakeholders);

        return emails.Count == emails.Distinct().Count();
    }

    private async Task<bool> BeFreeOfExistingEmails(
        CreateStakeholderGroupCommand command,
        List<NewStakeholderInput> stakeholders,
        CancellationToken cancellationToken)
    {
        if (stakeholders is null || stakeholders.Count == 0) return true;

        var emails = ValidAddresses(stakeholders).Distinct().ToList();

        if (emails.Count == 0) return true;

        return command.ProjectId is not null
            ? !await _readDb.Stakeholders
                .AnyAsync(x => x.ProjectId == command.ProjectId && emails.Contains(x.Email), cancellationToken)
            : !await _readDb.Stakeholders
                .AnyAsync(x => x.ProjectId == null && emails.Contains(x.Email), cancellationToken);
    }

    /// Rows that are not valid addresses are skipped here; NewStakeholderInputValidator reports those.
    private static List<EmailAddress> ValidAddresses(IEnumerable<NewStakeholderInput> stakeholders) =>
        stakeholders
            .Select(x => EmailAddress.Of(x.Email))
            .Where(x => x.IsSuccess)
            .Select(x => x.Value)
            .ToList();
}

public class CreateStakeholderGroupHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<CreateStakeholderGroupCommand, Result<StakeholderGroupId>>
{
    public async ValueTask<Result<StakeholderGroupId>> Handle(
        CreateStakeholderGroupCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsMandatory)
        {
            var projectExists = await dbContext.Projects
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.ProjectId, cancellationToken);

            if (!projectExists)
                return Result<StakeholderGroupId>.Error("Project not found.");
        }

        var actor = await currentUser.ProvideAsync();

        var group = request.IsMandatory
            ? StakeholderGroup.Mandatory(
                StakeholderGroupId.New(), request.Name, request.Description, actor, clock)
            : StakeholderGroup.ForProject(
                StakeholderGroupId.New(), request.ProjectId, request.Name, request.Description, actor, clock);

        // A group with no stakeholders is legitimate - every row on the form may be blank.
        foreach (var stakeholder in request.Stakeholders ?? [])
        {
            var email = EmailAddress.Of(stakeholder.Email);
            if (email.IsFailure) return Result<StakeholderGroupId>.Error(email.Message);

            var added = group.AddStakeholder(StakeholderId.New(), stakeholder.Name, email.Value, actor, clock);
            if (added.IsFailure) return Result<StakeholderGroupId>.Error(added.Message);
        }

        dbContext.StakeholderGroups.Add(group);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result<StakeholderGroupId>.OK(group.Id);
    }
}
