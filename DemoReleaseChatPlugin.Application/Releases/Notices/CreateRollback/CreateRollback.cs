using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Notices.CreateRollback;

/// No version and no project: both are inherited from the release being reverted.
public record CreateRollbackCommand : IRequest<Result<FileNoticeResult>>
{
    public ReleaseNoticeId LiveReleaseId { get; set; }
    public DateTime? RolledBackOn { get; set; }
    public string RollbackReason { get; set; }
}

/// <summary>
/// "Has this release already been rolled back?" stays a validator rule, not an aggregate invariant:
/// the answer lives in a DIFFERENT row (a Rollback pointing back at this release), not in the
/// LiveRelease's own state, so the aggregate cannot see it. The rules that ARE about the release's
/// own state - a rollback dated before the deployment, a missing reason - moved to Rollback.Record.
/// </summary>
public class CreateRollbackValidator : AbstractValidator<CreateRollbackCommand>
{
    private readonly IDemoReleaseChatPluginReadDb _readDb;

    public CreateRollbackValidator(IDemoReleaseChatPluginReadDb readDb)
    {
        _readDb = readDb;

        RuleFor(x => x.LiveReleaseId)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Please choose the release being rolled back.")
            .MustAsync(LiveReleaseExists).WithMessage("The release being rolled back could not be found.")
            .MustAsync(NotAlreadyRolledBack).WithMessage("This release has already been rolled back.");

        RuleFor(x => x.RolledBackOn).NotNull().WithMessage("Please give the rollback date and time.");

        RuleFor(x => x.RollbackReason)
            .NotEmpty().WithMessage("Please explain why the release was rolled back.")
            .MaximumLength(2000);
    }

    private async Task<bool> LiveReleaseExists(ReleaseNoticeId liveReleaseId, CancellationToken cancellationToken) =>
        await _readDb.LiveReleases.AnyAsync(x => x.Id == liveReleaseId, cancellationToken);

    private async Task<bool> NotAlreadyRolledBack(ReleaseNoticeId liveReleaseId, CancellationToken cancellationToken) =>
        !await _readDb.Rollbacks.AnyAsync(x => x.RollbackOfReleaseId == liveReleaseId, cancellationToken);
}

public class CreateRollbackHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<CreateRollbackCommand, Result<FileNoticeResult>>
{
    public async ValueTask<Result<FileNoticeResult>> Handle(
        CreateRollbackCommand request, CancellationToken cancellationToken)
    {
        var liveRelease = await dbContext.LiveReleases.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.LiveReleaseId, cancellationToken);

        if (liveRelease is null)
            return Result<FileNoticeResult>.Error("The release being rolled back could not be found.");

        var projectExists = await dbContext.Projects.AsNoTracking()
            .AnyAsync(x => x.Id == liveRelease.ProjectId, cancellationToken);

        if (!projectExists)
            return Result<FileNoticeResult>.Error("Project not found.");

        var actor = await currentUser.ProvideAsync();

        var notice = Rollback.Record(
            liveRelease, ReleaseNoticeId.New(), request.RolledBackOn!.Value, request.RollbackReason, actor, clock);

        if (notice.IsFailure)
            return Result<FileNoticeResult>.Error(notice.Message);

        dbContext.ReleaseNotices.Add(notice.Value);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result<FileNoticeResult>.OK(new FileNoticeResult { ReleaseNoticeId = notice.Value.Id });
    }
}
