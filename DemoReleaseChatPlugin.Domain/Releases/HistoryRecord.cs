namespace DemoReleaseChatPlugin.Domain.Releases;

/// <summary>
/// One line of the human-readable timeline shown in the "View history" dialog.
/// <c>CorrelationId</c> is deliberately NOT a foreign key and deliberately NOT one of the typed
/// identifiers: it holds a project id OR a stakeholder-group id, and one event writes two rows so
/// the same table is read from both the project page and the group dialog. A raw
/// <see cref="Guid"/> is the honest type for a column that is genuinely polymorphic.
/// </summary>
public sealed class HistoryRecord
{
    public Guid Id { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string Description { get; private set; }
    public DateTime HappenedOn { get; private set; }
    public string HappenedBy { get; private set; }

    private HistoryRecord() { }

    public HistoryRecord(Guid correlationId, string description, DateTime happenedOn, string happenedBy)
    {
        Id = Guid.CreateVersion7();
        CorrelationId = correlationId;
        Description = description;
        HappenedOn = happenedOn;
        HappenedBy = happenedBy;
    }
}
