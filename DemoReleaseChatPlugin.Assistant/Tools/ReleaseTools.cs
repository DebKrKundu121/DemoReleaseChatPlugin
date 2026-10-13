using System.ComponentModel;
using System.Globalization;
using DemoReleaseChatPlugin.Application.Releases.Notices.GetApplicationReleases;
using DemoReleaseChatPlugin.Application.Releases.Notices.GetReleaseHistory;
using DemoReleaseChatPlugin.Application.Releases.Projects.GetProjects;
using DemoReleaseChatPlugin.Assistant.Canvas;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using Mediator;

namespace DemoReleaseChatPlugin.Assistant.Tools;

/// Projects and releases. Offered only when the user can open the release pages.
///
/// Every tool goes through the same Mediator query the pages use — no tool has data access of its own,
/// so the assistant sees exactly what a person would, with the same rules applied.
///
/// The list tools also stash their rows in the workspace data store and return its 'source' token, so
/// present_table can show the real rows as a Verified table without the model re-typing them.
public sealed class ReleaseTools(ISender sender, IWorkspaceDataStore dataStore)
{
    public const int MaxNotices = 50;

    /// Release notes can be long prose; a cap keeps one lookup from flooding the model's context.
    private const int MaxDetailLength = 500;

    [Description("Lists the projects (applications) this system tracks release notices for, with each " +
                 "project's id, name, description, stakeholder count and number of releases. Call this " +
                 "for questions about which projects or applications exist, and FIRST whenever you need a " +
                 "project's id or its exact name for another tool.")]
    public Task<object> ListProjects(
        [Description("True to include archived projects. Leave false unless the user asks about archived or old projects.")]
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
        => ToolExecutor.RunAsync(async () =>
        {
            var result = await sender.Send(new GetProjectsQuery { IncludeArchived = includeArchived }, cancellationToken);

            if (result.IsFailure)
            {
                return ToolExecutor.Error(result);
            }

            var projects = result.Value.Projects
                .Select(p => new
                {
                    id = p.Id.Value,
                    name = p.Name,
                    description = p.Description,
                    isArchived = p.IsArchived,
                    stakeholders = p.StakeholderCount,
                    releases = p.ReleaseCount
                })
                .ToList();

            return new
            {
                total = projects.Count,
                source = dataStore.StashRows(projects, "id"),      // the id is for tools, not people
                projects
            };
        });

    [Description("Returns release notices, newest filed first, for one project or for all projects. Kinds: " +
                 "'Scheduled' (a release announced in advance, with a risk level), 'Live' (a go-live: the " +
                 "release was deployed) and 'Rollback' (a go-live was reverted). 'date' is the planned date " +
                 "for Scheduled, the deployment date for Live and the rollback date for Rollback. 'projectId' " +
                 "must be an id returned by list_projects. Returns at most 50 notices; when " +
                 "'truncated' is true, say so and suggest narrowing by project or kind.")]
    public Task<object> GetReleaseHistory(
        [Description("The project's id from list_projects. Omit for every project.")]
        string projectId = null,
        [Description("'Scheduled', 'Live' or 'Rollback'. Omit for all three.")]
        string kind = null,
        CancellationToken cancellationToken = default)
        => ToolExecutor.RunAsync(async () =>
        {
            ProjectId? project = null;

            if (!string.IsNullOrWhiteSpace(projectId))
            {
                if (!Guid.TryParse(projectId, out var id))
                {
                    return ToolExecutor.Error($"'{projectId}' is not a project id. Call list_projects and use the 'id' it returns.");
                }

                project = new ProjectId(id);
            }

            // The query returns nothing at all for a kind it doesn't recognise, which would read as
            // "no releases". Check it here so a wrong kind becomes an error the model can correct.
            string? kindValue = null;

            if (!string.IsNullOrWhiteSpace(kind))
            {
                var match = ReleaseNoticeKind.All.FirstOrDefault(k => string.Equals(k.Value, kind.Trim(), StringComparison.OrdinalIgnoreCase));

                if (match is null)
                {
                    return ToolExecutor.Error($"'{kind}' is not a kind. Use 'Scheduled', 'Live' or 'Rollback', or omit it for all three.");
                }

                kindValue = match.Value;
            }

            var result = await sender.Send(new GetReleaseHistoryQuery { ProjectId = project, Kind = kindValue }, cancellationToken);

            if (result.IsFailure)
            {
                return ToolExecutor.Error(result);
            }

            var all = result.Value.Notices;
            var notices = all
                .Take(MaxNotices)
                .Select(n => new
                {
                    kind = n.Kind,
                    project = n.ProjectName,
                    version = n.Version,
                    date = Date(n.EffectiveOn),
                    leadDeveloper = n.LeadDeveloper,
                    risk = n.RiskLevel?.Value,             // Scheduled only
                    isRolledBack = n.IsRolledBack,         // Live only
                    wasAnnounced = n.WasAnnounced,         // Live only: a Scheduled notice came first
                    filedOn = Date(n.CreatedOn),
                    detail = Trim(n.Detail)
                })
                .ToList();

            return new
            {
                total = all.Count,
                returned = notices.Count,
                truncated = all.Count > notices.Count,
                source = dataStore.StashRows(notices, "detail"),   // prose would swamp the grid
                notices
            };
        });

    [Description("Returns the most recent go-live or rollback for one project — the version it is on now. " +
                 "Scheduled releases are not included because they have not happened yet. " +
                 "'applicationName' must match a project name exactly (see list_projects); if it is not " +
                 "found, call list_projects and try again with the exact name.")]
    public Task<object> GetLatestRelease(
        [Description("The project's exact name, as list_projects returns it.")]
        string applicationName,
        CancellationToken cancellationToken = default)
        => ToolExecutor.RunAsync(async () =>
        {
            var result = await sender.Send(new GetLatestApplicationReleaseQuery { ApplicationName = applicationName }, cancellationToken);

            if (!result.IsSuccess)
            {
                return ToolExecutor.Error(result);      // not found, or the read failed
            }

            var release = result.Value;

            return new
            {
                kind = release.Kind,
                application = release.ApplicationName,
                version = release.Version,
                date = Date(release.OccurredOn),
                leadDeveloper = release.LeadDeveloper,
                isRolledBack = release.IsRolledBack,
                filedOn = Date(release.RecordedOn),
                detail = Trim(release.Detail)
            };
        });

    /// yyyy-MM-dd: an unambiguous wire format for the model; it writes DD/MM/YYYY for people.
    private static string Date(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Trim(string text)
        => text is { Length: > MaxDetailLength } ? text[..MaxDetailLength] + "…" : text;
}
