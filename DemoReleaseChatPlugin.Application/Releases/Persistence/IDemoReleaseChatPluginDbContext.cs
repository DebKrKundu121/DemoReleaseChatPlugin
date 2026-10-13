using DemoReleaseChatPlugin.Domain.Authorization;
using DemoReleaseChatPlugin.Domain.Releases;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Persistence;

/// <summary>
/// The WRITE side. Command handlers depend on this, never on the concrete context; anything that
/// only reads takes <see cref="IDemoReleaseChatPluginReadDb"/> instead, which has no way to write.
/// It lives under Releases/Persistence even though Administration uses it too - that is where it is.
///
/// The three subtype DbSets over one TPH table are what let a command say
/// <c>dbContext.LiveReleases.FirstOrDefaultAsync(...)</c> and get the discriminator filter for free.
///
/// <c>SaveChangesAsync</c> is on here because the unit of work needs it. Command handlers still must
/// not call it - they stage changes and call
/// <see cref="IDemoReleaseChatPluginUnitOfWork.CommitAsync"/>.
/// </summary>
public interface IDemoReleaseChatPluginDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }

    DbSet<Project> Projects { get; }
    DbSet<StakeholderGroup> StakeholderGroups { get; }
    DbSet<Stakeholder> Stakeholders { get; }
    DbSet<ReleaseNotice> ReleaseNotices { get; }
    DbSet<ScheduledRelease> ScheduledReleases { get; }
    DbSet<LiveRelease> LiveReleases { get; }
    DbSet<Rollback> Rollbacks { get; }
    DbSet<ProjectDocument> ProjectDocuments { get; }

    DbSet<DomainEventRecord> DomainEvents { get; }
    DbSet<HistoryRecord> HistoryRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
