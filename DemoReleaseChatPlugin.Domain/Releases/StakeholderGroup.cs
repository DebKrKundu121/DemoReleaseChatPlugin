using DemoReleaseChatPlugin.Domain.Releases.Events;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Domain.Releases;

/// <summary>
/// The aggregate root for stakeholders. Two named factories and no public constructor:
/// <see cref="ForProject"/> and <see cref="Mandatory"/> are the only two shapes a group can have,
/// and the private constructor makes an invalid third shape unrepresentable.
/// </summary>
public sealed class StakeholderGroup : Entity<StakeholderGroupId>, IAggregateRoot
{
    private readonly List<Stakeholder> _stakeholders = [];

    /// NULL means a mandatory (organisation-wide) group. The `?` is load-bearing: EF reads the
    /// property's nullability to decide whether the column is NOT NULL, and the unique index on
    /// (ProjectId, Name) uses HasFilter(null) so that single NULL counts as a value.
    public ProjectId? ProjectId { get; private set; }
    public bool IsMandatory { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public string CreatedBy { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTime? ModifiedOn { get; private set; }
    public string ModifiedBy { get; private set; }

    public IReadOnlyCollection<Stakeholder> Stakeholders => _stakeholders;

    private StakeholderGroup() { }  // EF

    private StakeholderGroup(
        StakeholderGroupId id, ProjectId? projectId, bool isMandatory, string name, string description,
        Actor actor, IClock clock)
    {
        var now = clock.Now;

        Id = id;
        ProjectId = projectId;
        IsMandatory = isMandatory;
        Name = name?.Trim();
        Description = description?.Trim();
        IsActive = true;
        CreatedOn = now;
        CreatedBy = actor.DisplayName;
        CreatedByUserId = actor.UserId;
        ModifiedOn = now;
        ModifiedBy = actor.DisplayName;

        AddDomainEvent(new StakeholderGroupCreated
        {
            OccurredBy = actor.DisplayName,
            StakeholderGroupId = Id,
            ProjectId = ProjectId,
            Name = Name
        }, clock);
    }

    public static StakeholderGroup ForProject(
        StakeholderGroupId id, ProjectId projectId, string name, string description, Actor actor, IClock clock) =>
        new(id, projectId, false, name, description, actor, clock);

    public static StakeholderGroup Mandatory(
        StakeholderGroupId id, string name, string description, Actor actor, IClock clock) =>
        new(id, null, true, name, description, actor, clock);

    /// <summary>
    /// The only way to construct a <see cref="Stakeholder"/>. Adding to a deactivated group is
    /// refused here rather than in a validator, because whether the group is active is this
    /// aggregate's own state and no caller should be able to work around the check.
    /// </summary>
    public Result<Stakeholder> AddStakeholder(
        StakeholderId id, string name, EmailAddress email, Actor actor, IClock clock)
    {
        if (!IsActive)
            return Result<Stakeholder>.Error(
                "This stakeholder group is inactive, so people cannot be added to it. Reactivate it first.");

        if (_stakeholders.Any(x => x.IsActive && x.Email == email))
            return Result<Stakeholder>.Error($"'{email}' is already in this group.");

        var stakeholder = Stakeholder.For(id, this, name, email, actor, clock);

        _stakeholders.Add(stakeholder);
        Stamp(actor, clock);

        return Result<Stakeholder>.OK(stakeholder);
    }

    public Result UpdateDetails(string name, string description, Actor actor, IClock clock)
    {
        if (!IsActive)
            return Result.Error("This stakeholder group is inactive, so its details cannot be changed. " +
                                "Reactivate it first.");

        var previousName = Name;
        var previousDescription = Description;

        Name = name?.Trim();
        Description = description?.Trim();
        Stamp(actor, clock);

        var nameChanged = !string.Equals(previousName, Name, StringComparison.Ordinal);
        var descriptionChanged = !string.Equals(previousDescription, Description, StringComparison.Ordinal);

        if (!nameChanged && !descriptionChanged)
            return Result.OK();

        AddDomainEvent(new StakeholderGroupDetailsUpdated
        {
            OccurredBy = actor.DisplayName,
            StakeholderGroupId = Id,
            ProjectId = ProjectId,
            PreviousName = nameChanged ? previousName : null,
            Name = Name,
            DescriptionChanged = descriptionChanged
        }, clock);

        return Result.OK();
    }

    public Result Deactivate(Actor actor, IClock clock)
    {
        if (!IsActive)
            return Result.Error("This stakeholder group is already inactive.");

        IsActive = false;
        Stamp(actor, clock);
        AddDomainEvent(new StakeholderGroupDeactivated
        {
            OccurredBy = actor.DisplayName,
            StakeholderGroupId = Id,
            ProjectId = ProjectId,
            Name = Name
        }, clock);

        return Result.OK();
    }

    public Result Reactivate(Actor actor, IClock clock)
    {
        if (IsActive)
            return Result.Error("This stakeholder group is already active.");

        IsActive = true;
        Stamp(actor, clock);
        AddDomainEvent(new StakeholderGroupReactivated
        {
            OccurredBy = actor.DisplayName,
            StakeholderGroupId = Id,
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
