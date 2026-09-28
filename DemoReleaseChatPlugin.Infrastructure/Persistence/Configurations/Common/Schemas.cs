namespace DemoReleaseChatPlugin.Infrastructure.Persistence.Configurations.Common;

/// <summary>
/// The four schemas this application owns. The fifth, <c>cap</c>, belongs to DotNetCore.CAP, which
/// creates and versions its own tables on startup - they are never in your migrations.
/// </summary>
public static class Schemas
{
    public const string Releases = "Releases";
    public const string Audit = "Audit";
    public const string History = "History";
    public const string Authorization = "Authorization";
}
