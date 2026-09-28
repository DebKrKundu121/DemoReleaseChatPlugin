using DemoReleaseChatPlugin.Domain.Releases.Events;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Domain.Releases;

/// <summary>
/// One table (TPH) holds all three notice kinds, discriminated by <see cref="Kind"/>.
/// Every subtype's columns must stay nullable in the database - a Scheduled row has no DeployedOn.
/// <para>
/// The base class cannot be sealed: the discriminator IS the inheritance. The three leaves are.
/// </para>
/// <para>
/// There is deliberately no <c>Project</c> navigation property any more, and no
/// <c>ScheduledRelease</c> / <c>RollbackOfRelease</c> one either. Each of those pointed at a
/// different aggregate root, and holding one meant any code with a notice in hand could reach
/// through and mutate a neighbouring aggregate in the same transaction. The foreign keys are still
/// there; only the object reference is gone, so a read that wants the project name joins for it.
/// </para>
/// </summary>
public abstract class ReleaseNotice : Entity<ReleaseNoticeId>, IAggregateRoot
{
    public ReleaseNoticeKind Kind { get; private set; }
    public ProjectId ProjectId { get; private set; }
    public ReleaseEnvironment TargetEnvironment { get; private set; }

    /// Free-form text, not a parsed semver - teams here number releases differently.
    public string Version { get; private set; }

    public string LeadDeveloper { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public string CreatedBy { get; private set; }
    public string CreatedByUserId { get; private set; }

    protected ReleaseNotice() { }

    protected ReleaseNotice(
        ReleaseNoticeId id,
        ReleaseNoticeKind kind,
        ProjectId projectId,
        string version,
        string leadDeveloper,
        Actor actor,
        IClock clock)
    {
        Id = id;
        Kind = kind;
        ProjectId = projectId;
        TargetEnvironment = ReleaseEnvironment.Production;
        Version = version?.Trim();
        LeadDeveloper = leadDeveloper?.Trim();
        CreatedOn = clock.Now;
        CreatedBy = actor.DisplayName;
        CreatedByUserId = actor.UserId;
    }
}

public sealed class ScheduledRelease : ReleaseNotice
{
    public DateTime ScheduledFor { get; private set; }
    public string Summary { get; private set; }
    public RiskLevel RiskLevel { get; private set; }
    public string RiskNotes { get; private set; }

    private ScheduledRelease() { }

    private ScheduledRelease(
        ReleaseNoticeId id, ProjectId projectId, string version, DateTime scheduledFor, string summary,
        RiskLevel riskLevel, string riskNotes, string leadDeveloper, Actor actor, IClock clock)
        : base(id, ReleaseNoticeKind.Scheduled, projectId, version, leadDeveloper, actor, clock)
    {
        ScheduledFor = scheduledFor;
        Summary = summary?.Trim();
        RiskLevel = riskLevel;
        RiskNotes = riskLevel is not null ? riskNotes?.Trim() : null;   // notes only survive with a level

        AddDomainEvent(new ScheduledReleaseFiled
        {
            OccurredBy = actor.DisplayName,
            ReleaseNoticeId = Id,
            ProjectId = ProjectId
        }, clock);
    }

    /// <summary>
    /// Takes the whole <see cref="Project"/> even though only its id is stored, because the notice
    /// cannot be filed without asking the project whether it is still open. The project is consulted
    /// and then forgotten - what is kept is <see cref="ReleaseNotice.ProjectId"/>.
    /// </summary>
    public static Result<ScheduledRelease> File(
        Project project, ReleaseNoticeId id, string version, DateTime scheduledFor, string summary,
        RiskLevel riskLevel, string riskNotes, string leadDeveloper, Actor actor, IClock clock)
    {
        var canFile = project.EnsureCanTakeReleaseNotices();
        if (canFile.IsFailure) return Result<ScheduledRelease>.Error(canFile.Message);

        if (string.IsNullOrWhiteSpace(version))
            return Result<ScheduledRelease>.Error("A release notice must name a version.");

        return Result<ScheduledRelease>.OK(new ScheduledRelease(
            id, project.Id, version, scheduledFor, summary, riskLevel, riskNotes, leadDeveloper, actor, clock));
    }
}

public sealed class LiveRelease : ReleaseNotice
{
    public DateTime DeployedOn { get; private set; }
    public string ReleaseNotes { get; private set; }

    /// Links this deployment to the scheduled notice that announced it. Null for an unplanned
    /// hotfix - and the `?` is what keeps the column nullable.
    public ReleaseNoticeId? ScheduledReleaseId { get; private set; }

    private LiveRelease() { }

    private LiveRelease(
        ReleaseNoticeId id, ProjectId projectId, string version, DateTime deployedOn, string releaseNotes,
        ReleaseNoticeId? scheduledReleaseId, string leadDeveloper, Actor actor, IClock clock)
        : base(id, ReleaseNoticeKind.Live, projectId, version, leadDeveloper, actor, clock)
    {
        DeployedOn = deployedOn;
        ReleaseNotes = releaseNotes?.Trim();
        ScheduledReleaseId = scheduledReleaseId;

        AddDomainEvent(new LiveReleaseRecorded
        {
            OccurredBy = actor.DisplayName,
            ReleaseNoticeId = Id,
            ProjectId = ProjectId
        }, clock);
    }

    public static Result<LiveRelease> Record(
        Project project, ReleaseNoticeId id, string version, DateTime deployedOn, string releaseNotes,
        ReleaseNoticeId? scheduledReleaseId, string leadDeveloper, Actor actor, IClock clock)
    {
        var canFile = project.EnsureCanTakeReleaseNotices();
        if (canFile.IsFailure) return Result<LiveRelease>.Error(canFile.Message);

        if (string.IsNullOrWhiteSpace(version))
            return Result<LiveRelease>.Error("A release notice must name a version.");

        return Result<LiveRelease>.OK(new LiveRelease(
            id, project.Id, version, deployedOn, releaseNotes, scheduledReleaseId, leadDeveloper, actor, clock));
    }
}

/// <summary>
/// A rollback is never given its own version. It inherits Version, ProjectId and LeadDeveloper from
/// the LiveRelease it reverts, so a revert cannot name a version that never shipped.
/// </summary>
public sealed class Rollback : ReleaseNotice
{
    public DateTime RolledBackOn { get; private set; }
    public string RollbackReason { get; private set; }
    public ReleaseNoticeId RollbackOfReleaseId { get; private set; }

    private Rollback() { }

    private Rollback(
        ReleaseNoticeId id, LiveRelease rolledBackRelease, DateTime rolledBackOn, string reason,
        Actor actor, IClock clock)
        : base(id, ReleaseNoticeKind.Rollback, rolledBackRelease.ProjectId, rolledBackRelease.Version,
               rolledBackRelease.LeadDeveloper, actor, clock)
    {
        RollbackOfReleaseId = rolledBackRelease.Id;
        RolledBackOn = rolledBackOn;
        RollbackReason = reason?.Trim();

        AddDomainEvent(new ReleaseRolledBack
        {
            OccurredBy = actor.DisplayName,
            ReleaseNoticeId = Id,
            ProjectId = ProjectId
        }, clock);
    }

    public static Result<Rollback> Record(
        LiveRelease rolledBackRelease, ReleaseNoticeId id, DateTime rolledBackOn, string reason,
        Actor actor, IClock clock)
    {
        if (rolledBackOn < rolledBackRelease.DeployedOn)
            return Result<Rollback>.Error(
                "A release cannot be rolled back before it went live. Check the rollback date and time.");

        if (string.IsNullOrWhiteSpace(reason))
            return Result<Rollback>.Error("Please explain why the release was rolled back.");

        return Result<Rollback>.OK(new Rollback(id, rolledBackRelease, rolledBackOn, reason, actor, clock));
    }
}
