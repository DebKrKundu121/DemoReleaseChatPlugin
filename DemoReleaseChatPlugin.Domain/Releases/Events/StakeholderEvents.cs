using DemoReleaseChatPlugin.Domain.Releases.Values;

namespace DemoReleaseChatPlugin.Domain.Releases.Events;

public sealed class StakeholderAdded : AdministrationDomainEvent
{
    public StakeholderId StakeholderId { get; set; }
    public StakeholderGroupId StakeholderGroupId { get; set; }
    public ProjectId? ProjectId { get; set; }
    public string GroupName { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public sealed class StakeholderDetailsUpdated : AdministrationDomainEvent
{
    public StakeholderId StakeholderId { get; set; }
    public StakeholderGroupId StakeholderGroupId { get; set; }
    public ProjectId? ProjectId { get; set; }

    /// Null when unchanged.
    public string PreviousName { get; set; }
    public string Name { get; set; }

    /// Null when unchanged.
    public string PreviousEmail { get; set; }
    public string Email { get; set; }
}

public sealed class StakeholderDeactivated : AdministrationDomainEvent
{
    public StakeholderId StakeholderId { get; set; }
    public StakeholderGroupId StakeholderGroupId { get; set; }
    public ProjectId? ProjectId { get; set; }
    public string Name { get; set; }
}

public sealed class StakeholderReactivated : AdministrationDomainEvent
{
    public StakeholderId StakeholderId { get; set; }
    public StakeholderGroupId StakeholderGroupId { get; set; }
    public ProjectId? ProjectId { get; set; }
    public string Name { get; set; }
}
