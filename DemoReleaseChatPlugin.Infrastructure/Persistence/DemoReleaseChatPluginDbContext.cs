using System.Reflection;
using DemoReleaseChatPlugin.Domain.Authorization;
using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Application.Releases.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence;

public class DemoReleaseChatPluginDbContext : DbContext, IDemoReleaseChatPluginDbContext
{
    public DemoReleaseChatPluginDbContext(DbContextOptions<DemoReleaseChatPluginDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }

    public DbSet<Project> Projects { get; set; }
    public DbSet<StakeholderGroup> StakeholderGroups { get; set; }
    public DbSet<Stakeholder> Stakeholders { get; set; }
    public DbSet<ReleaseNotice> ReleaseNotices { get; set; }
    public DbSet<ScheduledRelease> ScheduledReleases { get; set; }
    public DbSet<LiveRelease> LiveReleases { get; set; }
    public DbSet<Rollback> Rollbacks { get; set; }

    public DbSet<DomainEventRecord> DomainEvents { get; set; }
    public DbSet<HistoryRecord> HistoryRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
