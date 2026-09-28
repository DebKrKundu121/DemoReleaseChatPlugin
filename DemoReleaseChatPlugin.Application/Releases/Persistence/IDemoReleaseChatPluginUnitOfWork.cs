namespace DemoReleaseChatPlugin.Application.Releases.Persistence;

/// <summary>
/// Command handlers NEVER call SaveChangesAsync. They stage changes and call CommitAsync, which in
/// one transaction saves rows, copies every domain event into Audit.DomainEvents, and publishes
/// those events. Getting that order wrong fails silently.
/// </summary>
public interface IDemoReleaseChatPluginUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken);
}
