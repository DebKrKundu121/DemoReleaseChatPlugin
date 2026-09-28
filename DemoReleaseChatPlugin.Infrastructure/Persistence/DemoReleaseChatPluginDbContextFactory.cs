using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence;

/// <summary>
/// A design-time factory that bypasses the Website host, so `dotnet ef` never triggers Key Vault.
/// Because it exists, both --project and --startup-project point at Infrastructure.
/// </summary>
public sealed class DemoReleaseChatPluginDbContextFactory : IDesignTimeDbContextFactory<DemoReleaseChatPluginDbContext>
{
    private const string LocalDbFallback =
        "Server=localhost;Database=DemoReleaseChatPlugin;Trusted_Connection=True;TrustServerCertificate=True";

    public DemoReleaseChatPluginDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DEMORELEASECHATPLUGIN_CONNECTION") ?? LocalDbFallback;

        var options = new DbContextOptionsBuilder<DemoReleaseChatPluginDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(DemoReleaseChatPluginDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            })
            .Options;

        return new DemoReleaseChatPluginDbContext(options);
    }
}
