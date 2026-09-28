using DemoReleaseChatPlugin.Contracts.Releases.V1.Common;
using DemoReleaseChatPlugin.Contracts.Releases.V1.GetReleases;
using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Notices.GetApplicationReleases;

public record GetApplicationReleasesQuery : IRequest<QueryResult<GetReleasesResult>>
{
    public string ApplicationName { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public record GetLatestApplicationReleaseQuery : IRequest<QueryResult<ReleaseDto>>
{
    public string ApplicationName { get; set; }
}

/// <summary>
/// The two API-facing queries. They project straight into the CONTRACT types, so there is one shape
/// on the wire, not two - which is why Core references Contracts.
///
/// They return QueryResult rather than Result because "no application called that" and "the read
/// broke" are different answers, and the controller has to turn them into different status codes.
/// This layer reports the fact; the controller decides what it means on the wire.
///
/// Go-lives and rollbacks only: a scheduled notice has not happened yet, so it is not something
/// another system should act on. A rollback counts as the latest release when it is the most recent
/// thing that happened - a consumer needs to know the revert more than the go-live it replaced.
/// </summary>
public class GetApplicationReleasesHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetApplicationReleasesQuery, QueryResult<GetReleasesResult>>
{
    public async ValueTask<QueryResult<GetReleasesResult>> Handle(
        GetApplicationReleasesQuery request, CancellationToken cancellationToken)
    {
        var project = await ApplicationReleases.FindProjectAsync(
            readDb, request.ApplicationName, cancellationToken);

        if (project is null)
            return QueryResult<GetReleasesResult>.NotFound(
                ApplicationReleases.UnknownApplication(request.ApplicationName));

        var all = await ApplicationReleases.LoadAsync(readDb, project, cancellationToken);

        // Computed BEFORE date filtering, so a window query still reports the current release.
        var latestReleaseId = all.Count > 0 ? all[0].Id : (Guid?)null;

        var filtered = all.AsEnumerable();

        // Whole-day filtering: >= from.Date and < to.Date.AddDays(1).
        if (request.From.HasValue)
            filtered = filtered.Where(x => x.OccurredOn >= request.From.Value.Date);

        if (request.To.HasValue)
            filtered = filtered.Where(x => x.OccurredOn < request.To.Value.Date.AddDays(1));

        return QueryResult<GetReleasesResult>.Of(new GetReleasesResult
        {
            ApplicationName = project.Name,
            Releases = filtered.ToList(),
            From = request.From,
            To = request.To,
            LatestReleaseId = latestReleaseId
        });
    }
}

public class GetLatestApplicationReleaseHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetLatestApplicationReleaseQuery, QueryResult<ReleaseDto>>
{
    public async ValueTask<QueryResult<ReleaseDto>> Handle(
        GetLatestApplicationReleaseQuery request, CancellationToken cancellationToken)
    {
        var project = await ApplicationReleases.FindProjectAsync(
            readDb, request.ApplicationName, cancellationToken);

        if (project is null)
            return QueryResult<ReleaseDto>.NotFound(
                ApplicationReleases.UnknownApplication(request.ApplicationName));

        var all = await ApplicationReleases.LoadAsync(readDb, project, cancellationToken);

        if (all.Count == 0)
            return QueryResult<ReleaseDto>.NotFound($"'{project.Name}' has no recorded releases yet.");

        return QueryResult<ReleaseDto>.Of(all[0]);
    }
}

internal static class ApplicationReleases
{
    internal record ProjectKey(ProjectId Id, string Name);

    /// <summary>
    /// The project NAME is the integration key - there is no separate slug or id for consumers to
    /// hold. A mismatch is the one failure mode worth making loud.
    /// </summary>
    internal static string UnknownApplication(string applicationName) =>
        $"No application named '{applicationName}' is registered in DemoReleaseChatPlugin. " +
        "The project name must match the application's name exactly.";

    internal static async Task<ProjectKey?> FindProjectAsync(
        IDemoReleaseChatPluginReadDb readDb, string applicationName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(applicationName)) return null;

        var name = applicationName.Trim();

        var project = await readDb.Projects
            .Where(x => x.Name == name)
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync(cancellationToken);

        return project is null ? null : new ProjectKey(project.Id, project.Name);
    }

    /// <summary>
    /// Ordering is OccurredOn desc, then RecordedOn desc.
    /// <para>
    /// Each kind is read into an intermediate shape first and mapped to the contract DTO in memory.
    /// The ids are value-converted columns, and <c>Contracts</c> is a dependency-free
    /// netstandard2.0 package that cannot see the typed-id types - so the unwrapping to a raw
    /// <see cref="Guid"/> has to happen in C#, after materialisation, not inside the SQL projection.
    /// The <c>IsRolledBack</c> sub-select still runs in the database, so this is two statements, not
    /// N+1.
    /// </para>
    /// </summary>
    internal static async Task<List<ReleaseDto>> LoadAsync(
        IDemoReleaseChatPluginReadDb readDb, ProjectKey project, CancellationToken cancellationToken)
    {
        var rolledBackIds = readDb.Rollbacks.Select(x => x.RollbackOfReleaseId);

        var liveRows = await readDb.LiveReleases
            .Where(x => x.ProjectId == project.Id)
            .Select(x => new
            {
                x.Id,
                x.Version,
                x.DeployedOn,
                x.ReleaseNotes,
                x.LeadDeveloper,
                IsRolledBack = rolledBackIds.Contains(x.Id),
                x.CreatedOn
            })
            .ToListAsync(cancellationToken);

        var lives = liveRows.Select(x => new ReleaseDto
        {
            Id = x.Id.Value,
            Kind = ReleaseKinds.Live,
            ApplicationName = project.Name,
            Version = x.Version,
            OccurredOn = x.DeployedOn,
            Detail = x.ReleaseNotes,
            LeadDeveloper = x.LeadDeveloper,
            IsRolledBack = x.IsRolledBack,
            RecordedOn = x.CreatedOn
        });

        var rollbackRows = await readDb.Rollbacks
            .Where(x => x.ProjectId == project.Id)
            .Select(x => new
            {
                x.Id,
                x.Version,
                x.RolledBackOn,
                x.RollbackReason,
                x.LeadDeveloper,
                x.RollbackOfReleaseId,
                x.CreatedOn
            })
            .ToListAsync(cancellationToken);

        var rollbacks = rollbackRows.Select(x => new ReleaseDto
        {
            Id = x.Id.Value,
            Kind = ReleaseKinds.Rollback,
            ApplicationName = project.Name,
            Version = x.Version,
            OccurredOn = x.RolledBackOn,
            Detail = x.RollbackReason,
            LeadDeveloper = x.LeadDeveloper,
            RollbackOfReleaseId = x.RollbackOfReleaseId.Value,
            RecordedOn = x.CreatedOn
        });

        return lives
            .Concat(rollbacks)
            .OrderByDescending(x => x.OccurredOn)
            .ThenByDescending(x => x.RecordedOn)
            .ToList();
    }
}
