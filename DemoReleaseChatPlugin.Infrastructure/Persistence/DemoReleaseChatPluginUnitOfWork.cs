using System.Text.Json;
using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DotNetCore.CAP;
using DoverPort.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Infrastructure.Persistence;

/// <summary>
/// The single most important class in the module. Command handlers stage changes and call
/// CommitAsync; this is the only place that saves, audits and publishes - and the ordering is
/// load-bearing.
/// </summary>
public class DemoReleaseChatPluginUnitOfWork(
    DemoReleaseChatPluginDbContext dbContext,
    ICapPublisher publisher,
    IClock clock)
    : IDemoReleaseChatPluginUnitOfWork
{
    public async Task<int> CommitAsync(CancellationToken cancellationToken)
    {
        var domainEvents = CollectDomainEvents();

        // CAP's overload, not EF's. It enlists the CAP outbox in the same transaction, so a
        // published message and the rows that caused it commit or roll back together.
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(publisher, autoCommit: false, cancellationToken);

        // Serialised with ReleaseSerializerOptions so the typed ids and closed vocabularies inside an
        // event are written as bare strings, exactly as the raw Guids and enum names were before
        // them. Audit.DomainEvents is append-only, so the stored shape has to stay stable.
        foreach (var domainEvent in domainEvents)
        {
            dbContext.DomainEvents.Add(new DomainEventRecord(
                domainEvent,
                JsonSerializer.Serialize(
                    domainEvent, domainEvent.GetType(), ReleaseSerializerOptions.Default),
                clock));
        }

        var rowsWritten = await dbContext.SaveChangesAsync(cancellationToken);

        // The topic is the event's CLR TYPE NAME, which is why subscribers use
        // [CapSubscribe(nameof(ScheduledReleaseFiled))]. Rename an event class and you silently
        // break its listener - there is no compile-time link.
        //
        // Publishing happens after the save but BEFORE the commit: a publish outside the transaction
        // could announce a release that then rolled back.
        foreach (var domainEvent in domainEvents)
        {
            await publisher.PublishAsync(
                domainEvent.GetType().Name, domainEvent, cancellationToken: cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return rowsWritten;
    }

    /// <summary>
    /// Collected BEFORE SaveChangesAsync and cleared immediately, so a re-entrant commit can't
    /// publish them twice. Entries&lt;Entity&lt;Guid&gt;&gt;() only sees entities deriving from
    /// Entity&lt;Guid&gt;: User and Role are plain POCOs, so Administration writes raise no events
    /// and produce no audit rows - intentional.
    /// </summary>
    private List<IDomainEvent> CollectDomainEvents()
    {
        var entities = dbContext.ChangeTracker
            .Entries<Entity<Guid>>()
            .Select(x => x.Entity)
            .Where(x => x.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = entities.SelectMany(x => x.DomainEvents).ToList();

        entities.ForEach(x => x.ClearDomainEvents());

        return domainEvents;
    }
}
