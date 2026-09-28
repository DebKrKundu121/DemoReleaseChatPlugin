using DemoReleaseChatPlugin.Domain.Releases.Values;

namespace DemoReleaseChatPlugin.Domain.Releases.Events;

public sealed class ProjectCreated : AdministrationDomainEvent
{
    public ProjectId ProjectId { get; set; }
    public string Name { get; set; }
}

public sealed class ProjectDetailsUpdated : AdministrationDomainEvent
{
    public ProjectId ProjectId { get; set; }

    /// Null when the name did not change - that is what lets the history listener write
    /// "renamed from 'X' to 'Y'" only when a rename actually happened.
    public string PreviousName { get; set; }

    public string Name { get; set; }
    public bool DescriptionChanged { get; set; }
}

public sealed class ProjectArchived : AdministrationDomainEvent
{
    public ProjectId ProjectId { get; set; }
    public string Name { get; set; }
}

public sealed class ProjectRestored : AdministrationDomainEvent
{
    public ProjectId ProjectId { get; set; }
    public string Name { get; set; }
}
