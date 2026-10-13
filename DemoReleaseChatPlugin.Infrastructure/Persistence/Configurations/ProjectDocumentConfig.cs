using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations;

public class ProjectDocumentConfig : IEntityTypeConfiguration<ProjectDocument>
{
    public void Configure(EntityTypeBuilder<ProjectDocument> builder)
    {
        builder.ToTable("ProjectDocuments", Schemas.Releases);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasIdConversion(ProjectDocumentId.From).ValueGeneratedNever().HasColumnName("Id");

        builder.Property(x => x.ProjectId).HasIdConversion(ProjectId.From).IsRequired().HasColumnName("ProjectId");

        builder.Property(x => x.FileName).IsRequired().HasMaxLength(255).HasColumnName("FileName");
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100).HasColumnName("ContentType");
        builder.Property(x => x.SizeBytes).IsRequired().HasColumnName("SizeBytes");
        builder.Property(x => x.Kind).HasVocabularyConversion().IsRequired().HasMaxLength(20).HasColumnName("Kind");
        builder.Property(x => x.PageCount).IsRequired().HasColumnName("PageCount");

        // The Domain project has nullable reference types on, so EF reads a plain `string` as NOT NULL.
        // The optional columns say IsRequired(false) explicitly.
        builder.Property(x => x.Summary).IsRequired(false).HasMaxLength(2000).HasColumnName("Summary");
        builder.Property(x => x.ExtractedText).IsRequired().HasColumnName("ExtractedText");    // nvarchar(max)
        builder.Property(x => x.Entities).IsRequired(false).HasColumnName("Entities");          // nvarchar(max), JSON

        builder.Property(x => x.OcrProvider).IsRequired(false).HasMaxLength(50).HasColumnName("OcrProvider");
        builder.Property(x => x.NerProvider).IsRequired().HasMaxLength(50).HasColumnName("NerProvider");

        builder.Property(x => x.MatchConfidence).HasPrecision(4, 3).HasColumnName("MatchConfidence");
        builder.Property(x => x.MatchReason).IsRequired(false).HasMaxLength(1000).HasColumnName("MatchReason");

        builder.Property(x => x.AddedOn).IsRequired().HasColumnType("datetime2(3)").HasColumnName("AddedOn");
        builder.Property(x => x.AddedBy).IsRequired().HasMaxLength(120).HasColumnName("AddedBy");
        builder.Property(x => x.AddedByUserId).IsRequired().HasMaxLength(64).HasColumnName("AddedByUserId");

        builder.HasIndex(x => new { x.ProjectId, x.AddedOn })
            .HasDatabaseName("IX_ProjectDocuments_ProjectId_AddedOn");

        // No navigation, for the same reason as ReleaseNotice: Project is a separate aggregate root.
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
