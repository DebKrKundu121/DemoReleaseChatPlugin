using DemoReleaseChatPlugin.Domain.Releases.Events;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Domain.Releases;

/// <summary>
/// Entity&lt;StakeholderId&gt; but deliberately NOT IAggregateRoot - it belongs to the group.
/// The constructor is private and the only way to create one is
/// <see cref="StakeholderGroup.AddStakeholder"/>, which is what keeps GroupId, ProjectId and
/// IsMandatory consistent with the group.
/// </summary>
public sealed class Stakeholder : Entity<StakeholderId>
{
    public StakeholderGroupId GroupId { get; private set; }

    /// Denormalised from the group at creation, on purpose: both the audience resolver and the
    /// uniqueness index need "who is in scope for project X, and who is organisation-wide?"
    /// without joining through the group on every query. Set once, never changed - a stakeholder
    /// cannot move between groups; you deactivate and re-add.
    /// Nullable for the same reason as StakeholderGroup.ProjectId: a stakeholder on a mandatory
    /// group belongs to no project.
    public ProjectId? ProjectId { get; private set; }

    public bool IsMandatory { get; private set; }
    public string Name { get; private set; }
    public EmailAddress Email { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public string CreatedBy { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTime? ModifiedOn { get; private set; }
    public string ModifiedBy { get; private set; }

    private Stakeholder() { }   // EF

    private Stakeholder(
        StakeholderId id, StakeholderGroup group, string name, EmailAddress email, Actor actor, IClock clock)
    {
        var now = clock.Now;

        Id = id;
        GroupId = group.Id;
        ProjectId = group.ProjectId;
        IsMandatory = group.IsMandatory;
        Name = name?.Trim();
        Email = email;
        IsActive = true;
        CreatedOn = now;
        CreatedBy = actor.DisplayName;
        CreatedByUserId = actor.UserId;
        ModifiedOn = now;
        ModifiedBy = actor.DisplayName;

        AddDomainEvent(new StakeholderAdded
        {
            OccurredBy = actor.DisplayName,
            StakeholderId = Id,
            StakeholderGroupId = GroupId,
            ProjectId = ProjectId,
            GroupName = group.Name,
            Name = Name,
            Email = Email.Value
        }, clock);
    }

    /// Internal, and called from one place only: <see cref="StakeholderGroup.AddStakeholder"/>.
    internal static Stakeholder For(
        StakeholderId id, StakeholderGroup group, string name, EmailAddress email, Actor actor, IClock clock) =>
        new(id, group, name, email, actor, clock);

    public Result UpdateDetails(string name, EmailAddress email, Actor actor, IClock clock)
    {
        if (!IsActive)
            return Result.Error("This stakeholder is inactive, so their details cannot be changed. " +
                                "Reactivate them first.");

        var previousName = Name;
        var previousEmail = Email;

        Name = name?.Trim();
        Email = email;
        Stamp(actor, clock);

        var nameChanged = !string.Equals(previousName, Name, StringComparison.Ordinal);
        var emailChanged = previousEmail != Email;

        if (!nameChanged && !emailChanged)
            return Result.OK();

        AddDomainEvent(new StakeholderDetailsUpdated
        {
            OccurredBy = actor.DisplayName,
            StakeholderId = Id,
            StakeholderGroupId = GroupId,
            ProjectId = ProjectId,
            PreviousName = nameChanged ? previousName : null,
            Name = Name,
            PreviousEmail = emailChanged ? previousEmail?.Value : null,
            Email = Email?.Value
        }, clock);

        return Result.OK();
    }

    public Result Deactivate(Actor actor, IClock clock)
    {
        if (!IsActive)
            return Result.Error("This stakeholder is already inactive.");

        IsActive = false;
        Stamp(actor, clock);
        AddDomainEvent(new StakeholderDeactivated
        {
            OccurredBy = actor.DisplayName,
            StakeholderId = Id,
            StakeholderGroupId = GroupId,
            ProjectId = ProjectId,
            Name = Name
        }, clock);

        return Result.OK();
    }

    public Result Reactivate(Actor actor, IClock clock)
    {
        if (IsActive)
            return Result.Error("This stakeholder is already active.");

        IsActive = true;
        Stamp(actor, clock);
        AddDomainEvent(new StakeholderReactivated
        {
            OccurredBy = actor.DisplayName,
            StakeholderId = Id,
            StakeholderGroupId = GroupId,
            ProjectId = ProjectId,
            Name = Name
        }, clock);

        return Result.OK();
    }

    private void Stamp(Actor actor, IClock clock)
    {
        ModifiedOn = clock.Now;
        ModifiedBy = actor.DisplayName;
    }
}
