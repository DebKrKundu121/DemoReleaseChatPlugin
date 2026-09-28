using DemoReleaseChatPlugin.Domain.Authorization;
using DemoReleaseChatPlugin.Domain.Releases;

namespace DemoReleaseChatPlugin.Application.Releases.Persistence;

/// <summary>
/// The read side. Query handlers and the read-only rules inside validators depend on this; command
/// handlers depend on <see cref="IDemoReleaseChatPluginDbContext"/> instead.
/// <para>
/// Two things the split buys, which a single shared context did not:
/// </para>
/// <list type="number">
/// <item>There is no <c>SaveChangesAsync</c> and no <c>Add</c> on this interface, so a query
/// physically cannot write. That used to be a convention enforced by review.</item>
/// <item>Every set is already <c>AsNoTracking</c>, so a read cannot accidentally pull an aggregate
/// into the change tracker and have it saved by a later unrelated commit - which is the failure
/// mode that makes a "read-only" query mutate data.</item>
/// </list>
/// <para>
/// <see cref="IQueryable{T}"/> rather than <c>DbSet&lt;T&gt;</c> on purpose: the application layer
/// gets composable queries without the write surface, and without naming an EF type on the port.
/// </para>
/// </summary>
public interface IDemoReleaseChatPluginReadDb
{
    IQueryable<User> Users { get; }
    IQueryable<Role> Roles { get; }

    IQueryable<Project> Projects { get; }
    IQueryable<StakeholderGroup> StakeholderGroups { get; }
    IQueryable<Stakeholder> Stakeholders { get; }

    /// The three subtype sets over one TPH table are what let a query say
    /// <c>readDb.LiveReleases.Where(...)</c> and get the discriminator filter for free.
    IQueryable<ReleaseNotice> ReleaseNotices { get; }
    IQueryable<ScheduledRelease> ScheduledReleases { get; }
    IQueryable<LiveRelease> LiveReleases { get; }
    IQueryable<Rollback> Rollbacks { get; }

    IQueryable<DomainEventRecord> DomainEvents { get; }
    IQueryable<HistoryRecord> HistoryRecords { get; }
}
