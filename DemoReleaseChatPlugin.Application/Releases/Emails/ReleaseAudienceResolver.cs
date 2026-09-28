using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Emails;

public interface IReleaseAudienceResolver
{
    Task<IReadOnlyCollection<EmailRecipient>> MandatoryOnlyAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EmailRecipient>> MandatoryAndStakeholdersAsync(
        ProjectId projectId, CancellationToken cancellationToken);
}

/// <summary>
/// The single place that decides who is emailed.
///
/// The mandatory list is resolved at SEND TIME and never copied onto a project: no per-project rows,
/// no backfill, no drift. Adding an address covers projects that already exist, and changing one does
/// not have to be corrected project by project.
/// </summary>
public class ReleaseAudienceResolver(IDemoReleaseChatPluginReadDb readDb) : IReleaseAudienceResolver
{
    public async Task<IReadOnlyCollection<EmailRecipient>> MandatoryOnlyAsync(CancellationToken cancellationToken) =>
        Deduplicate(await MandatoryRecipientsAsync(cancellationToken));

    public async Task<IReadOnlyCollection<EmailRecipient>> MandatoryAndStakeholdersAsync(
        ProjectId projectId, CancellationToken cancellationToken)
    {
        var mandatory = await MandatoryRecipientsAsync(cancellationToken);

        var stakeholders = await readDb.Stakeholders
            .Where(x => x.ProjectId == projectId && x.IsActive && ActiveGroupIds().Contains(x.GroupId))
            .OrderBy(x => x.Name)
            .Select(x => new EmailRecipient
            {
                Audience = NotificationAudience.Stakeholder,
                Name = x.Name,
                Email = x.Email
            })
            .ToListAsync(cancellationToken);

        return Deduplicate(mandatory.Concat(stakeholders));
    }

    private async Task<List<EmailRecipient>> MandatoryRecipientsAsync(CancellationToken cancellationToken) =>
        await readDb.Stakeholders
            .Where(x => x.IsMandatory && x.IsActive && ActiveGroupIds().Contains(x.GroupId))
            .OrderBy(x => x.Name)
            .Select(x => new EmailRecipient
            {
                Audience = NotificationAudience.Mandatory,
                Name = x.Name,
                Email = x.Email
            })
            .ToListAsync(cancellationToken);

    /// Two independent active flags: a stakeholder is only emailed if BOTH they and their group are
    /// active. Deactivating a group silences everyone in it without touching individual rows.
    private IQueryable<StakeholderGroupId> ActiveGroupIds() =>
        readDb.StakeholderGroups.Where(x => x.IsActive).Select(x => x.Id);

    /// Mandatory comes first in the concat, so someone on both lists is emailed once, tagged Mandatory.
    private static IReadOnlyCollection<EmailRecipient> Deduplicate(IEnumerable<EmailRecipient> recipients) =>
        recipients
            .Where(x => x.Email is not null)
            // EmailAddress is already normalised to lower case at creation, so a plain group-by on
            // the value object is the case-insensitive comparison this used to spell out by hand.
            .GroupBy(x => x.Email)
            .Select(group => group.First())
            .ToList();
}
