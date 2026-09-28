using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Domain.Releases;

/// <summary>
/// The append-only audit row, one per domain event. Written ONLY by the unit of work - never by a
/// handler. Stored as JSON rather than typed columns because events differ and new ones arrive:
/// columns would mean a migration each time and a table of mostly-nulls. It is for reading, not for
/// querying by field.
/// <para>
/// Not an aggregate and not <c>Entity&lt;T&gt;</c> - it has no invariants, raises nothing, and is
/// never loaded to be changed. Its id and <see cref="RecordedOn"/> are passed in rather than read
/// from the ambient clock, so this project contains no call to <c>DateTime.Now</c> at all.
/// </para>
/// </summary>
public sealed class DomainEventRecord
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Details { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public DateTime RecordedOn { get; private set; }
    public string ActionedBy { get; private set; }

    private DomainEventRecord() { }

    public DomainEventRecord(IDomainEvent domainEvent, string details, IClock clock)
    {
        Id = Guid.CreateVersion7();
        Name = domainEvent.GetType().Name;
        Details = details;
        OccurredOn = domainEvent.OccurredOn;
        RecordedOn = clock.Now;
        ActionedBy = domainEvent.OccurredBy;
    }
}
