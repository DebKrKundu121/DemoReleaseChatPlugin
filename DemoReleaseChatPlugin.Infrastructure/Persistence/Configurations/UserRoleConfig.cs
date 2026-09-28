using DemoReleaseChatPlugin.Domain.Authorization;
using DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations;

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", Schemas.Authorization);

        // Plain HasKey, no ValueGeneratedNever: Id is DB-generated here, unlike every Releases entity.
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnName("Name");
        builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(100).HasColumnName("ExternalId");

        // EF's default join-table column names (RolesId / UsersId) are what the seed script depends on.
        builder.HasMany(x => x.Roles)
            .WithMany(x => x.Users)
            .UsingEntity(e => e.ToTable("UserRoles", Schemas.Authorization));

        builder.HasIndex(x => x.ExternalId).IsUnique().HasDatabaseName("UX_Users_ExternalId");
    }
}

public class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", Schemas.Authorization);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnName("Name");
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500).HasColumnName("Description");

        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_Roles_Name");
    }
}
