using DemoReleaseChatPlugin.Domain.Releases.Values;

namespace DemoReleaseChatPlugin.Domain.Releases.Events;

public sealed class StakeholderGroupCreated : AdministrationDomainEvent
{
    public StakeholderGroupId StakeholderGroupId { get; set; }
    public ProjectId? ProjectId { get; set; }
    public string Name { get; set; }
}

public sealed class StakeholderGroupDetailsUpdated : AdministrationDomainEvent
{
    public StakeholderGroupId StakeholderGroupId { get; set; }
    public ProjectId? ProjectId { get; set; }
    public string PreviousName { get; set; }
    public string Name { get; set; }
    public bool DescriptionChanged { get; set; }
}

public sealed class StakeholderGroupDeactivated : AdministrationDomainEvent
{
    public StakeholderGroupId StakeholderGroupId { get; set; }
    public ProjectId? ProjectId { get; set; }
    public string Name { get; set; }
}

public sealed class StakeholderGroupReactivated : AdministrationDomainEvent
{
    public StakeholderGroupId StakeholderGroupId { get; set; }
    public ProjectId? ProjectId { get; set; }
    public string Name { get; set; }
}
