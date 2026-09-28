using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations;

/// No HasOne/HasMany here - the relationship is declared from the group side only.
public class StakeholderConfig : IEntityTypeConfiguration<Stakeholder>
{
    public void Configure(EntityTypeBuilder<Stakeholder> builder)
    {
        builder.ToTable("Stakeholders", Schemas.Releases);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasIdConversion(StakeholderId.From).ValueGeneratedNever().HasColumnName("Id");

        builder.Property(x => x.GroupId).HasIdConversion(StakeholderGroupId.From).IsRequired().HasColumnName("GroupId");
        builder.Property(x => x.ProjectId).HasOptionalIdConversion(ProjectId.From).HasColumnName("ProjectId");
        builder.Property(x => x.IsMandatory).IsRequired().HasColumnName("IsMandatory");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150).HasColumnName("Name");
        builder.Property(x => x.Email).HasEmailConversion().IsRequired().HasMaxLength(EmailAddress.MaxLength).HasColumnName("Email");
        builder.Property(x => x.IsActive).IsRequired().HasColumnName("IsActive");

        builder.Property(x => x.CreatedOn).IsRequired().HasColumnType("datetime2(3)").HasColumnName("CreatedOn");
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(120).HasColumnName("CreatedBy");
        builder.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(64).HasColumnName("CreatedByUserId");
        builder.Property(x => x.ModifiedOn).HasColumnType("datetime2(3)").HasColumnName("ModifiedOn");
        builder.Property(x => x.ModifiedBy).HasMaxLength(120).HasColumnName("ModifiedBy");

        builder.HasIndex(x => x.GroupId).HasDatabaseName("IX_Stakeholders_GroupId");
        builder.HasIndex(x => x.ProjectId).HasDatabaseName("IX_Stakeholders_ProjectId");

        // Same HasFilter(null) trick as the group: one email can only appear once per project scope,
        // and once across all mandatory groups.
        builder.HasIndex(x => new { x.ProjectId, x.Email })
            .IsUnique()
            .HasFilter(null)
            .HasDatabaseName("UX_Stakeholders_ProjectId_Email");
    }
}
