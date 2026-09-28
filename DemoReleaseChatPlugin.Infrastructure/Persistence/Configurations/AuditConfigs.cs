using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations;

public class DomainEventRecordConfig : IEntityTypeConfiguration<DomainEventRecord>
{
    public void Configure(EntityTypeBuilder<DomainEventRecord> builder)
    {
        builder.ToTable("DomainEvents", Schemas.Audit);

        // Non-clustered PK on a random GUID avoids page splits on a table that is only ever appended
        // to and read chronologically.
        builder.HasKey(x => x.Id).HasAnnotation("SqlServer:Clustered", false);

        builder.Property(x => x.Id).ValueGeneratedNever().HasColumnName("Id");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(250).HasColumnName("Name");
        builder.Property(x => x.Details).IsRequired().HasColumnName("Details");
        builder.Property(x => x.OccurredOn).IsRequired().HasColumnType("datetime2(3)").HasColumnName("OccurredOn");
        builder.Property(x => x.RecordedOn).IsRequired().HasColumnType("datetime2(3)").HasColumnName("RecordedOn");
        builder.Property(x => x.ActionedBy).IsRequired(false).HasMaxLength(250).HasColumnName("ActionedBy");

        builder.HasIndex(x => x.OccurredOn).HasDatabaseName("IX_DomainEvents_OccurredOn");
        builder.HasIndex(x => x.Name).HasDatabaseName("IX_DomainEvents_Name");
    }
}

public class HistoryRecordConfig : IEntityTypeConfiguration<HistoryRecord>
{
    public void Configure(EntityTypeBuilder<HistoryRecord> builder)
    {
        builder.ToTable("HistoryRecords", Schemas.History);

        builder.HasKey(x => x.Id).HasAnnotation("SqlServer:Clustered", false);

        builder.Property(x => x.Id).ValueGeneratedNever().HasColumnName("Id");
        builder.Property(x => x.CorrelationId).IsRequired().HasColumnName("CorrelationId");
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000).HasColumnName("Description");
        builder.Property(x => x.HappenedOn).IsRequired().HasColumnType("datetime2(3)").HasColumnName("HappenedOn");
        builder.Property(x => x.HappenedBy).IsRequired(false).HasMaxLength(250).HasColumnName("HappenedBy");

        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_HistoryRecords_CorrelationId");
        builder.HasIndex(x => x.HappenedOn).HasDatabaseName("IX_HistoryRecords_HappenedOn");
    }
}
