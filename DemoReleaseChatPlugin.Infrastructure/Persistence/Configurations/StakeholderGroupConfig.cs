using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations;

public class StakeholderGroupConfig : IEntityTypeConfiguration<StakeholderGroup>
{
    public void Configure(EntityTypeBuilder<StakeholderGroup> builder)
    {
        builder.ToTable("StakeholderGroups", Schemas.Releases);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasIdConversion(StakeholderGroupId.From).ValueGeneratedNever().HasColumnName("Id");

        builder.Property(x => x.ProjectId).HasOptionalIdConversion(ProjectId.From).HasColumnName("ProjectId");
        builder.Property(x => x.IsMandatory).IsRequired().HasColumnName("IsMandatory");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150).HasColumnName("Name");
        builder.Property(x => x.Description).HasMaxLength(500).HasColumnName("Description");
        builder.Property(x => x.IsActive).IsRequired().HasColumnName("IsActive");

        builder.Property(x => x.CreatedOn).IsRequired().HasColumnType("datetime2(3)").HasColumnName("CreatedOn");
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(120).HasColumnName("CreatedBy");
        builder.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(64).HasColumnName("CreatedByUserId");
        builder.Property(x => x.ModifiedOn).HasColumnType("datetime2(3)").HasColumnName("ModifiedOn");
        builder.Property(x => x.ModifiedBy).HasMaxLength(120).HasColumnName("ModifiedBy");

        builder.HasIndex(x => x.ProjectId).HasDatabaseName("IX_StakeholderGroups_ProjectId");

        // HasFilter(null) is deliberate. EF's default for a unique index over a nullable column is a
        // filtered WHERE [ProjectId] IS NOT NULL, which would let two mandatory groups share a name.
        // Passing null removes the filter, so SQL Server treats the single NULL as one value and the
        // uniqueness covers mandatory groups too.
        builder.HasIndex(x => new { x.ProjectId, x.Name })
            .IsUnique()
            .HasFilter(null)
            .HasDatabaseName("UX_StakeholderGroups_ProjectId_Name");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Stakeholders)
            .WithOne()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Lets the aggregate expose IReadOnlyCollection<Stakeholder> over a private
        // List<Stakeholder> _stakeholders while EF still populates and tracks it.
        builder.Metadata
            .FindNavigation(nameof(StakeholderGroup.Stakeholders))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
