using DemoReleaseChatPlugin.Domain.Releases.Values;

namespace DemoReleaseChatPlugin.Application.Releases.StakeholderGroups;

public record StakeholderGroupDto
{
    public StakeholderGroupId Id { get; set; }
    public ProjectId? ProjectId { get; set; }
    public bool IsMandatory { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; }

    public IReadOnlyList<GroupStakeholderDto> Stakeholders { get; set; } = [];

    public int StakeholderCount => Stakeholders.Count;
    public int ActiveStakeholderCount => Stakeholders.Count(x => x.IsActive);

    public string StakeholderSummary =>
        StakeholderCount == ActiveStakeholderCount
            ? $"{StakeholderCount}"
            : $"{ActiveStakeholderCount} of {StakeholderCount} active";
}

/// <summary>
/// A class, not a record - the stakeholder grid edits it in place.
/// <para>
/// <c>Email</c> is an <see cref="EmailAddress"/> rather than a string because the projection reads
/// the column straight through the value converter. A screen that needs the raw text for a textbox
/// takes <c>.Value</c> once it has been materialised.
/// </para>
/// </summary>
public class GroupStakeholderDto
{
    public StakeholderId Id { get; set; }
    public StakeholderGroupId GroupId { get; set; }
    public string Name { get; set; }
    public EmailAddress Email { get; set; }

    /// <summary>
    /// The raw text the inline grid edits, which a textbox can two-way bind to where an
    /// <see cref="EmailAddress"/> cannot. It reads through to <see cref="Email"/> until somebody
    /// types, so a row that is only displayed needs no extra work, and what gets posted back is
    /// whatever is actually in the box. The value object is rebuilt from it in the handler, which
    /// is the point at which unvalidated text stops being unvalidated text.
    /// </summary>
    public string EmailText
    {
        get => _emailText ?? Email?.Value;
        set => _emailText = value;
    }

    private string _emailText;

    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; }
}
