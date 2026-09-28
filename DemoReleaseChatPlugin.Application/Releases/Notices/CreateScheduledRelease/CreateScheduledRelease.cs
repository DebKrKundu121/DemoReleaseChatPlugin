using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Notices.CreateScheduledRelease;

public record CreateScheduledReleaseCommand : IRequest<Result<FileNoticeResult>>
{
    public ProjectId ProjectId { get; set; }
    public string Version { get; set; }
    public DateTime ScheduledFor { get; set; }
    public string Summary { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string RiskNotes { get; set; }
    public string LeadDeveloper { get; set; }
}

/// <summary>
/// Shape, and rules that can only be answered by looking at OTHER rows - does the project exist, is
/// this version already taken, is it newer than the last one. The rule about the project's own
/// state ("is it archived?") used to be here too; it now lives on <c>Project</c>, because that is
/// the object that knows, and a validator is skippable in a way an aggregate is not.
/// </summary>
public class CreateScheduledReleaseValidator : AbstractValidator<CreateScheduledReleaseCommand>
{
    private readonly IDemoReleaseChatPluginReadDb _readDb;

    public CreateScheduledReleaseValidator(IDemoReleaseChatPluginReadDb readDb)
    {
        _readDb = readDb;

        RuleFor(x => x.ProjectId)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Please choose a project.")
            .MustAsync(ProjectExists).WithMessage("Project not found.");

        RuleFor(x => x.Version)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Please give the version number.")
            .MaximumLength(50)
            .MustAsync(NotAlreadyScheduled)
                .WithMessage("This version is already scheduled for this project. " +
                             "Each version can only be scheduled once per project.");

        RuleFor(x => x.Version).CustomAsync(EnsureNewerThanLastRelease);

        RuleFor(x => x.ScheduledFor).NotNull().WithMessage("Please give the planned date and time.");

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("Please summarise what is being released.")
            .MaximumLength(2000);

        RuleFor(x => x.RiskNotes)
            .NotEmpty().WithMessage("Please describe the risk and its impact.")
            .MaximumLength(2000)
            .When(x => x.RiskLevel is not null);

        RuleFor(x => x.LeadDeveloper)
            .NotEmpty().WithMessage("Please name the lead developer.")
            .MaximumLength(150);
    }

    private async Task<bool> ProjectExists(ProjectId projectId, CancellationToken cancellationToken) =>
        await _readDb.Projects.AnyAsync(x => x.Id == projectId, cancellationToken);

    private async Task<bool> NotAlreadyScheduled(
        CreateScheduledReleaseCommand command, string version, CancellationToken cancellationToken)
    {
        var candidate = version?.Trim();

        return !await _readDb.ScheduledReleases
            .AnyAsync(x => x.ProjectId == command.ProjectId && x.Version == candidate, cancellationToken);
    }

    private async Task EnsureNewerThanLastRelease(
        string version,
        ValidationContext<CreateScheduledReleaseCommand> context,
        CancellationToken cancellationToken)
    {
        var failure = await ReleaseVersionRule.NotNewerThanLastReleaseAsync(
            _readDb, context.InstanceToValidate.ProjectId, version, cancellationToken);

        if (failure is not null)
            context.AddFailure(nameof(CreateScheduledReleaseCommand.Version), failure);
    }
}

/// Depends on nothing to do with email. Filing a notice and announcing it are separate concerns -
/// the entity raises ScheduledReleaseFiled and a CAP subscriber does the rest.
public class CreateScheduledReleaseHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<CreateScheduledReleaseCommand, Result<FileNoticeResult>>
{
    public async ValueTask<Result<FileNoticeResult>> Handle(
        CreateScheduledReleaseCommand request, CancellationToken cancellationToken)
    {
        // FirstOrDefault, not FirstAsync: the validator proved the project exists, but this handler
        // must not depend on a validator having run to avoid throwing.
        var project = await dbContext.Projects.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ProjectId, cancellationToken);

        if (project is null)
            return Result<FileNoticeResult>.Error("Project not found.");

        var actor = await currentUser.ProvideAsync();

        // The archived-project rule is enforced inside File(...), which is why this is a Result.
        var notice = ScheduledRelease.File(
            project, ReleaseNoticeId.New(), request.Version, request.ScheduledFor, request.Summary,
            request.RiskLevel, request.RiskNotes, request.LeadDeveloper, actor, clock);

        if (notice.IsFailure)
            return Result<FileNoticeResult>.Error(notice.Message);

        // Added to the base set - TPH resolves the subtype from the discriminator.
        dbContext.ReleaseNotices.Add(notice.Value);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result<FileNoticeResult>.OK(new FileNoticeResult { ReleaseNoticeId = notice.Value.Id });
    }
}
