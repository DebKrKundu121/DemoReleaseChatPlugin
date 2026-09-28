using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Domain.Releases.Events;

/// <summary>
/// Events about projects, groups and stakeholders. Named "Administration" for historical reasons -
/// they belong to the Releases module, not to the Administration module.
/// These DO carry names and emails, because their only listener writes a human-readable sentence
/// and would otherwise have to re-read a row that may have changed again.
/// </summary>
public abstract class AdministrationDomainEvent : IDomainEvent
{
    /// Both stamped by Entity.AddDomainEvent(event, clock) when the event is raised - NOT read
    /// from the ambient clock here, so an entity built against a fixed clock produces fixed events.
    public Guid Id { get; set; }
    public DateTime OccurredOn { get; set; }
    public string OccurredBy { get; set; }
}

/// <summary>
/// Events about release notices. These carry IDENTIFIERS ONLY - a listener that needs detail
/// re-reads the record, so the message stays small, the listener reads current data rather than a
/// snapshot frozen at publish time, and adding a field to a notice is not a change to every message
/// already queued. ProjectId is the one deliberate exception, so a listener can decide relevance
/// before hitting the database.
/// </summary>
public abstract class ReleaseDomainEvent : IDomainEvent
{
    /// Both stamped by Entity.AddDomainEvent(event, clock) when the event is raised - NOT read
    /// from the ambient clock here, so an entity built against a fixed clock produces fixed events.
    public Guid Id { get; set; }
    public DateTime OccurredOn { get; set; }
    public string OccurredBy { get; set; }
}
