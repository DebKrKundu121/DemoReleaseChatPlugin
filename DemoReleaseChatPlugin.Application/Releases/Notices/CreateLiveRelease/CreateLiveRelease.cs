using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Application.Releases.Publishing;
using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Notices.CreateLiveRelease;

public record CreateLiveReleaseCommand : IRequest<Result<FileNoticeResult>>
{
    public ProjectId ProjectId { get; set; }
    public string Version { get; set; }
    public DateTime? DeployedOn { get; set; }
    public string ReleaseNotes { get; set; }

    /// Links this deployment to the scheduled notice that announced it. Null for an unplanned hotfix.
    public ReleaseNoticeId? ScheduledReleaseId { get; set; }

    public string LeadDeveloper { get; set; }
}

public class CreateLiveReleaseValidator : AbstractValidator<CreateLiveReleaseCommand>
{
    private readonly IDemoReleaseChatPluginReadDb _readDb;

    public CreateLiveReleaseValidator(IDemoReleaseChatPluginReadDb readDb)
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
            .MustAsync(NotAlreadyLive)
                .WithMessage("This version has already been recorded as live for this project.");

        RuleFor(x => x.Version).CustomAsync(EnsureNewerThanLastRelease);

        RuleFor(x => x.DeployedOn).NotNull().WithMessage("Please give the actual go-live date and time.");

        RuleFor(x => x.ReleaseNotes).NotEmpty().WithMessage("Please provide the release notes.");

        RuleFor(x => x.ScheduledReleaseId)
            .MustAsync(BelongToTheProject)
            .WithMessage("The selected scheduled notice does not belong to this project.")
            .When(x => x.ScheduledReleaseId is not null);

        RuleFor(x => x.LeadDeveloper)
            .NotEmpty().WithMessage("Please name the lead developer.")
            .MaximumLength(150);
    }

    private async Task<bool> ProjectExists(ProjectId projectId, CancellationToken cancellationToken) =>
        await _readDb.Projects.AnyAsync(x => x.Id == projectId, cancellationToken);

    private async Task<bool> NotAlreadyLive(
        CreateLiveReleaseCommand command, string version, CancellationToken cancellationToken)
    {
        var candidate = version?.Trim();

        return !await _readDb.LiveReleases
            .AnyAsync(x => x.ProjectId == command.ProjectId && x.Version == candidate, cancellationToken);
    }

    private async Task<bool> BelongToTheProject(
        CreateLiveReleaseCommand command, ReleaseNoticeId? scheduledReleaseId, CancellationToken cancellationToken) =>
        await _readDb.ScheduledReleases
            .AnyAsync(
                x => x.Id == scheduledReleaseId && x.ProjectId == command.ProjectId,   // guarded by .When(is not null)
                cancellationToken);

    private async Task EnsureNewerThanLastRelease(
        string version,
        ValidationContext<CreateLiveReleaseCommand> context,
        CancellationToken cancellationToken)
    {
        var failure = await ReleaseVersionRule.NotNewerThanLastReleaseAsync(
            _readDb, context.InstanceToValidate.ProjectId, version, cancellationToken);

        if (failure is not null)
            context.AddFailure(nameof(CreateLiveReleaseCommand.Version), failure);
    }
}

public class CreateLiveReleaseHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork,
    IReleaseNotesPublisher publisher)
    : IRequestHandler<CreateLiveReleaseCommand, Result<FileNoticeResult>>
{
    public async ValueTask<Result<FileNoticeResult>> Handle(
        CreateLiveReleaseCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ProjectId, cancellationToken);

        if (project is null)
            return Result<FileNoticeResult>.Error("Project not found.");

        var actor = await currentUser.ProvideAsync();

        var notice = LiveRelease.Record(
            project, ReleaseNoticeId.New(), request.Version, request.DeployedOn!.Value,
            request.ReleaseNotes, request.ScheduledReleaseId, request.LeadDeveloper, actor, clock);

        if (notice.IsFailure)
            return Result<FileNoticeResult>.Error(notice.Message);

        dbContext.ReleaseNotices.Add(notice.Value);

        await unitOfWork.CommitAsync(cancellationToken);

        // After the commit, per the "side-effects happen after the commit" rule.
        await publisher.PublishAsync(
            notice.Value.Id, project.Name, notice.Value.ReleaseNotes, cancellationToken);

        return Result<FileNoticeResult>.OK(new FileNoticeResult { ReleaseNoticeId = notice.Value.Id });
    }
}
