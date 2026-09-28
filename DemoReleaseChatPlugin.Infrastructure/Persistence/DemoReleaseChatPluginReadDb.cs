using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Authorization;
using DemoReleaseChatPlugin.Domain.Releases;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence;

/// <summary>
/// The read-side adapter. It shares the request's <see cref="DemoReleaseChatPluginDbContext"/> - so
/// there is still one connection and one transaction per request - but hands out only
/// <c>AsNoTracking</c> queryables and no way to write.
/// <para>
/// Applying <c>AsNoTracking</c> here rather than at each call site is the point: a query cannot
/// forget it, so a read can no longer pull an aggregate into the change tracker and have it written
/// out by an unrelated <c>CommitAsync</c> later in the same request.
/// </para>
/// </summary>
public sealed class DemoReleaseChatPluginReadDb(DemoReleaseChatPluginDbContext dbContext)
    : IDemoReleaseChatPluginReadDb
{
    public IQueryable<User> Users => dbContext.Users.AsNoTracking();
    public IQueryable<Role> Roles => dbContext.Roles.AsNoTracking();

    public IQueryable<Project> Projects => dbContext.Projects.AsNoTracking();
    public IQueryable<StakeholderGroup> StakeholderGroups => dbContext.StakeholderGroups.AsNoTracking();
    public IQueryable<Stakeholder> Stakeholders => dbContext.Stakeholders.AsNoTracking();

    public IQueryable<ReleaseNotice> ReleaseNotices => dbContext.ReleaseNotices.AsNoTracking();
    public IQueryable<ScheduledRelease> ScheduledReleases => dbContext.ScheduledReleases.AsNoTracking();
    public IQueryable<LiveRelease> LiveReleases => dbContext.LiveReleases.AsNoTracking();
    public IQueryable<Rollback> Rollbacks => dbContext.Rollbacks.AsNoTracking();

    public IQueryable<DomainEventRecord> DomainEvents => dbContext.DomainEvents.AsNoTracking();
    public IQueryable<HistoryRecord> HistoryRecords => dbContext.HistoryRecords.AsNoTracking();
}
