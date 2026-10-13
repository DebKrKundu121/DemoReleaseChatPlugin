using DemoReleaseChatPlugin.Domain.Releases.Events;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Domain.Releases;

/// <summary>
/// Projects are archived, never deleted, so release history always resolves.
/// <c>Name</c> is the integration key: other applications call the public API with their own
/// application name and it must match this string exactly.
/// <para>
/// Sealed, with private setters, a private constructor and a named factory. Nothing outside this
/// class can put a Project into a state it did not choose - the only way in is
/// <see cref="Create"/>, and the only ways to change it are the methods below, each of which checks
/// its rule first.
/// </para>
/// </summary>
public sealed class Project : Entity<ProjectId>, IAggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public string CreatedBy { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTime? ModifiedOn { get; private set; }
    public string ModifiedBy { get; private set; }

    private Project() { }   // EF

    private Project(ProjectId id, string name, string description, Actor actor, IClock clock)
    {
        var now = clock.Now;

        Id = id;
        Name = name?.Trim();
        Description = description?.Trim();
        IsArchived = false;
        CreatedOn = now;
        CreatedBy = actor.DisplayName;
        CreatedByUserId = actor.UserId;
        ModifiedOn = now;
        ModifiedBy = actor.DisplayName;

        AddDomainEvent(new ProjectCreated { OccurredBy = actor.DisplayName, ProjectId = Id, Name = Name }, clock);
    }

    /// <summary>
    /// The only way to create a Project. The id and the clock are supplied by the caller rather than
    /// generated here, so the domain stays deterministic: a test constructs a Project at a known
    /// instant with a known id and gets the same events every run.
    /// </summary>
    public static Project Create(ProjectId id, string name, string description, Actor actor, IClock clock) =>
        new(id, name, description, actor, clock);

    /// <summary>
    /// An archived project is a closed record. Editing its name would silently re-point the
    /// integration key other applications call the public API with, so it has to be restored first.
    /// </summary>
    public Result UpdateDetails(string name, string description, Actor actor, IClock clock)
    {
        if (IsArchived)
            return Result.Error("This project is archived, so its details cannot be changed. Restore it first.");

        var previousName = Name;
        var previousDescription = Description;

        Name = name?.Trim();
        Description = description?.Trim();
        Stamp(actor, clock);

        var nameChanged = !string.Equals(previousName, Name, StringComparison.Ordinal);
        var descriptionChanged = !string.Equals(previousDescription, Description, StringComparison.Ordinal);

        if (!nameChanged && !descriptionChanged)
            return Result.OK();     // a no-op edit raises nothing

        AddDomainEvent(new ProjectDetailsUpdated
        {
            OccurredBy = actor.DisplayName,
            ProjectId = Id,
            PreviousName = nameChanged ? previousName : null,
            Name = Name,
            DescriptionChanged = descriptionChanged
        }, clock);

        return Result.OK();
    }

    public Result Archive(Actor actor, IClock clock)
    {
        if (IsArchived)
            return Result.Error("This project is already archived.");

        IsArchived = true;
        Stamp(actor, clock);
        AddDomainEvent(new ProjectArchived { OccurredBy = actor.DisplayName, ProjectId = Id, Name = Name }, clock);

        return Result.OK();
    }

    public Result Restore(Actor actor, IClock clock)
    {
        if (!IsArchived)
            return Result.Error("This project is not archived.");

        IsArchived = false;
        Stamp(actor, clock);
        AddDomainEvent(new ProjectRestored { OccurredBy = actor.DisplayName, ProjectId = Id, Name = Name }, clock);

        return Result.OK();
    }

    /// <summary>
    /// The rule "no release notices against an archived project", expressed once, on the object that
    /// knows whether it is archived. The notice factories call this before they build anything, so
    /// there is no path that files a notice without the check.
    /// </summary>
    internal Result EnsureCanTakeReleaseNotices() =>
        IsArchived
            ? Result.Error("This project is archived, so releases cannot be filed against it.")
            : Result.OK();

    /// The same rule for documents: an archived project is a closed record.
    internal Result EnsureCanTakeDocuments() =>
        IsArchived
            ? Result.Error("This project is archived, so documents cannot be attached to it.")
            : Result.OK();

    private void Stamp(Actor actor, IClock clock)
    {
        ModifiedOn = clock.Now;
        ModifiedBy = actor.DisplayName;
    }
}
