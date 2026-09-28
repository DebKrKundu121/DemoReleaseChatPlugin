using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations;

public class ReleaseNoticeConfig : IEntityTypeConfiguration<ReleaseNotice>
{
    public void Configure(EntityTypeBuilder<ReleaseNotice> builder)
    {
        builder.ToTable("ReleaseNotices", Schemas.Releases);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasIdConversion(ReleaseNoticeId.From).ValueGeneratedNever().HasColumnName("Id");

        // The discriminator is the vocabulary, converted to the same strings the column already
        // holds ("Scheduled" / "Live" / "Rollback"). The discriminator VALUES have to be the raw
        // strings: EF compares them against the stored column before any converter runs.
        builder.Property(x => x.Kind)
            .HasVocabularyConversion().IsRequired().HasMaxLength(20).HasColumnName("Kind");

        builder.HasDiscriminator(x => x.Kind)
            .HasValue<ScheduledRelease>(ReleaseNoticeKind.Scheduled)
            .HasValue<LiveRelease>(ReleaseNoticeKind.Live)
            .HasValue<Rollback>(ReleaseNoticeKind.Rollback);

        builder.Property(x => x.ProjectId).HasIdConversion(ProjectId.From).IsRequired().HasColumnName("ProjectId");

        builder.Property(x => x.TargetEnvironment)
            .HasVocabularyConversion().IsRequired().HasMaxLength(20).HasColumnName("TargetEnvironment");

        builder.Property(x => x.Version).IsRequired().HasMaxLength(50).HasColumnName("Version");
        builder.Property(x => x.LeadDeveloper).IsRequired().HasMaxLength(150).HasColumnName("LeadDeveloper");

        builder.Property(x => x.CreatedOn).IsRequired().HasColumnType("datetime2(3)").HasColumnName("CreatedOn");
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(120).HasColumnName("CreatedBy");
        builder.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(64).HasColumnName("CreatedByUserId");

        builder.HasIndex(x => new { x.ProjectId, x.CreatedOn })
            .HasDatabaseName("IX_ReleaseNotices_ProjectId_CreatedOn");

        // Kind is in the unique key on purpose. Scheduling 2.1.0 and then recording 2.1.0 going live
        // are two notices about the same version; a per-project-and-version constraint would block
        // the normal flow. With Kind in the key it stops a project scheduling 2.1.0 twice or
        // recording it live twice, and allows the schedule -> go-live pair.
        builder.HasIndex(x => new { x.ProjectId, x.Kind, x.Version })
            .IsUnique()
            .HasDatabaseName("UX_ReleaseNotices_ProjectId_Kind_Version");

        // Declared WITHOUT a navigation property. Project is a separate aggregate root, so a notice
        // holds its id and nothing else; the foreign key and its delete behaviour are unchanged.
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// NEVER mark a subtype column IsRequired. TPH puts every subtype's columns in one table, so they
/// must be nullable - a Scheduled row has no DeployedOn. Marking one required makes every insert of
/// the other kinds fail.
/// </summary>
public class ScheduledReleaseConfig : IEntityTypeConfiguration<ScheduledRelease>
{
    public void Configure(EntityTypeBuilder<ScheduledRelease> builder)
    {
        builder.Property(x => x.ScheduledFor).HasColumnType("datetime2(3)").HasColumnName("ScheduledFor");
        builder.Property(x => x.Summary).HasMaxLength(2000).HasColumnName("Summary");
        builder.Property(x => x.RiskLevel).HasVocabularyConversion().HasMaxLength(20).HasColumnName("RiskLevel");
        builder.Property(x => x.RiskNotes).HasMaxLength(2000).HasColumnName("RiskNotes");

        builder.HasIndex(x => x.ScheduledFor).HasDatabaseName("IX_ReleaseNotices_ScheduledFor");
    }
}

public class LiveReleaseConfig : IEntityTypeConfiguration<LiveRelease>
{
    public void Configure(EntityTypeBuilder<LiveRelease> builder)
    {
        builder.Property(x => x.DeployedOn).HasColumnType("datetime2(3)").HasColumnName("DeployedOn");
        builder.Property(x => x.ReleaseNotes).HasColumnName("ReleaseNotes");
        builder.Property(x => x.ScheduledReleaseId)
            .HasOptionalIdConversion(ReleaseNoticeId.From).HasColumnName("ScheduledReleaseId");

        // Self-referencing FK - MUST stay Restrict, or SQL Server rejects the model with a
        // multiple-cascade-path error. No navigation, for the same reason as Project above.
        builder.HasOne<ScheduledRelease>()
            .WithMany()
            .HasForeignKey(x => x.ScheduledReleaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RollbackConfig : IEntityTypeConfiguration<Rollback>
{
    public void Configure(EntityTypeBuilder<Rollback> builder)
    {
        builder.Property(x => x.RolledBackOn).HasColumnType("datetime2(3)").HasColumnName("RolledBackOn");
        builder.Property(x => x.RollbackReason).HasMaxLength(2000).HasColumnName("RollbackReason");
        builder.Property(x => x.RollbackOfReleaseId)
            .HasIdConversion(ReleaseNoticeId.From).HasColumnName("RollbackOfReleaseId");

        // The other self-referencing FK - also Restrict, for the same reason.
        builder.HasOne<LiveRelease>()
            .WithMany()
            .HasForeignKey(x => x.RollbackOfReleaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.RollbackOfReleaseId).HasDatabaseName("IX_ReleaseNotices_RollbackOfReleaseId");
    }
}
