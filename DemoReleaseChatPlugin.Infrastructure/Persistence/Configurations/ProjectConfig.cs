using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations;

public class ProjectConfig : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", Schemas.Releases);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasIdConversion(ProjectId.From).ValueGeneratedNever().HasColumnName("Id");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(150).HasColumnName("Name");
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_Projects_Name");

        builder.Property(x => x.Description).HasMaxLength(500).HasColumnName("Description");
        builder.Property(x => x.IsArchived).IsRequired().HasColumnName("IsArchived");

        builder.Property(x => x.CreatedOn).IsRequired().HasColumnType("datetime2(3)").HasColumnName("CreatedOn");
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(120).HasColumnName("CreatedBy");
        builder.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(64).HasColumnName("CreatedByUserId");
        builder.Property(x => x.ModifiedOn).HasColumnType("datetime2(3)").HasColumnName("ModifiedOn");
        builder.Property(x => x.ModifiedBy).HasMaxLength(120).HasColumnName("ModifiedBy");
    }
}
