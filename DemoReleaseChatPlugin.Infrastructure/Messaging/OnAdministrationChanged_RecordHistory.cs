using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Events;
using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DotNetCore.CAP;

namespace DemoReleaseChatPlugin.Infrastructure.Messaging;

/// <summary>
/// Turns eleven administration events into human-readable timeline rows.
///
/// Group = "History" puts these subscriptions in their own CAP consumer group, so they are
/// independent of any other subscriber to the same events.
///
/// This listener writes with dbContext.SaveChangesAsync directly rather than the unit of work - it
/// is not a domain operation, raises no events, and must not re-enter the publish path.
/// </summary>
public class OnAdministrationChanged_RecordHistory(IDemoReleaseChatPluginDbContext dbContext) : ICapSubscribe
{
    private const string HistoryGroup = "History";
    private const int MaxDescriptionLength = 1000;

    // ----- Projects -------------------------------------------------------------------------

    [CapSubscribe(nameof(ProjectCreated), Group = HistoryGroup)]
    public async Task Handle(ProjectCreated @event, CancellationToken cancellationToken) =>
        await RecordAsync(@event, $"Project '{@event.Name}' created.", cancellationToken, @event.ProjectId?.Value);

    [CapSubscribe(nameof(ProjectDetailsUpdated), Group = HistoryGroup)]
    public async Task Handle(ProjectDetailsUpdated @event, CancellationToken cancellationToken)
    {
        var parts = new List<string>();

        if (@event.PreviousName is not null)
            parts.Add($"Project renamed from '{@event.PreviousName}' to '{@event.Name}'.");

        if (@event.DescriptionChanged)
            parts.Add($"Project '{@event.Name}' description updated.");

        await RecordAsync(@event, string.Join(" ", parts), cancellationToken, @event.ProjectId?.Value);
    }

    [CapSubscribe(nameof(ProjectArchived), Group = HistoryGroup)]
    public async Task Handle(ProjectArchived @event, CancellationToken cancellationToken) =>
        await RecordAsync(@event, $"Project '{@event.Name}' archived.", cancellationToken, @event.ProjectId?.Value);

    [CapSubscribe(nameof(ProjectRestored), Group = HistoryGroup)]
    public async Task Handle(ProjectRestored @event, CancellationToken cancellationToken) =>
        await RecordAsync(@event, $"Project '{@event.Name}' restored.", cancellationToken, @event.ProjectId?.Value);

    // ----- Stakeholder groups ---------------------------------------------------------------

    [CapSubscribe(nameof(StakeholderGroupCreated), Group = HistoryGroup)]
    public async Task Handle(StakeholderGroupCreated @event, CancellationToken cancellationToken) =>
        await RecordAsync(@event, $"Stakeholder group '{@event.Name}' created.", cancellationToken,
            @event.StakeholderGroupId?.Value, @event.ProjectId?.Value);

    [CapSubscribe(nameof(StakeholderGroupDetailsUpdated), Group = HistoryGroup)]
    public async Task Handle(StakeholderGroupDetailsUpdated @event, CancellationToken cancellationToken)
    {
        var parts = new List<string>();

        if (@event.PreviousName is not null)
            parts.Add($"Stakeholder group renamed from '{@event.PreviousName}' to '{@event.Name}'.");

        if (@event.DescriptionChanged)
            parts.Add($"Stakeholder group '{@event.Name}' description updated.");

        await RecordAsync(@event, string.Join(" ", parts), cancellationToken,
            @event.StakeholderGroupId?.Value, @event.ProjectId?.Value);
    }

    [CapSubscribe(nameof(StakeholderGroupDeactivated), Group = HistoryGroup)]
    public async Task Handle(StakeholderGroupDeactivated @event, CancellationToken cancellationToken) =>
        await RecordAsync(@event, $"Stakeholder group '{@event.Name}' deactivated.", cancellationToken,
            @event.StakeholderGroupId?.Value, @event.ProjectId?.Value);

    [CapSubscribe(nameof(StakeholderGroupReactivated), Group = HistoryGroup)]
    public async Task Handle(StakeholderGroupReactivated @event, CancellationToken cancellationToken) =>
        await RecordAsync(@event, $"Stakeholder group '{@event.Name}' reactivated.", cancellationToken,
            @event.StakeholderGroupId?.Value, @event.ProjectId?.Value);

    // ----- Stakeholders ---------------------------------------------------------------------

    [CapSubscribe(nameof(StakeholderAdded), Group = HistoryGroup)]
    public async Task Handle(StakeholderAdded @event, CancellationToken cancellationToken) =>
        await RecordAsync(
            @event,
            $"Stakeholder '{@event.Name}' ({@event.Email}) added to group '{@event.GroupName}'.",
            cancellationToken,
            @event.StakeholderGroupId?.Value, @event.ProjectId?.Value);

    [CapSubscribe(nameof(StakeholderDetailsUpdated), Group = HistoryGroup)]
    public async Task Handle(StakeholderDetailsUpdated @event, CancellationToken cancellationToken)
    {
        var parts = new List<string>();

        if (@event.PreviousName is not null)
            parts.Add($"Stakeholder renamed from '{@event.PreviousName}' to '{@event.Name}'.");

        if (@event.PreviousEmail is not null)
            parts.Add($"Stakeholder '{@event.Name}' email changed from '{@event.PreviousEmail}' to '{@event.Email}'.");

        await RecordAsync(@event, string.Join(" ", parts), cancellationToken,
            @event.StakeholderGroupId?.Value, @event.ProjectId?.Value);
    }

    [CapSubscribe(nameof(StakeholderDeactivated), Group = HistoryGroup)]
    public async Task Handle(StakeholderDeactivated @event, CancellationToken cancellationToken) =>
        await RecordAsync(@event, $"Stakeholder '{@event.Name}' deactivated.", cancellationToken,
            @event.StakeholderGroupId?.Value, @event.ProjectId?.Value);

    [CapSubscribe(nameof(StakeholderReactivated), Group = HistoryGroup)]
    public async Task Handle(StakeholderReactivated @event, CancellationToken cancellationToken) =>
        await RecordAsync(@event, $"Stakeholder '{@event.Name}' reactivated.", cancellationToken,
            @event.StakeholderGroupId?.Value, @event.ProjectId?.Value);

    /// <summary>
    /// params Guid?[] because one event writes up to TWO rows - a stakeholder change is visible both
    /// on the group dialog's history and on the project's history. .Distinct() plus null/empty
    /// skipping means a mandatory group (no ProjectId) writes one row.
    /// An empty description writes nothing.
    /// <para>
    /// Raw <see cref="Guid"/> rather than a typed id on purpose, and the call sites unwrap with
    /// <c>?.Value</c> rather than leaning on the implicit conversion: HistoryRecord.CorrelationId
    /// holds a project id OR a group id, so there is no single typed id that is honest here, and
    /// letting a null id silently convert to <see cref="Guid.Empty"/> would hide the mandatory-group
    /// case instead of stating it.
    /// </para>
    /// </summary>
    private async Task RecordAsync(
        AdministrationDomainEvent @event,
        string description,
        CancellationToken cancellationToken,
        params Guid?[] correlationIds)
    {
        if (string.IsNullOrWhiteSpace(description))
            return;   // a no-op edit writes nothing

        var text = Fit(description);
        var written = false;

        foreach (var correlationId in correlationIds.Distinct())
        {
            if (!correlationId.HasValue || correlationId.Value == Guid.Empty)
                continue;

            dbContext.HistoryRecords.Add(new HistoryRecord(
                correlationId.Value, text, @event.OccurredOn, @event.OccurredBy));

            written = true;
        }

        if (written)
            await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Fit(string description) =>
        description.Length <= MaxDescriptionLength ? description : description[..MaxDescriptionLength];
}
