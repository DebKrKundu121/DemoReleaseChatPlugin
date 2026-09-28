using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Notices.Lookups;

// ---------------------------------------------------------------------------------------------
// Three small queries in one file. They feed the dropdowns on the three notice forms.
// ---------------------------------------------------------------------------------------------

public record GetProjectOptionsQuery : IRequest<Result<GetProjectOptionsResult>>;

public record GetProjectOptionsResult
{
    public IReadOnlyList<Option> Projects { get; set; } = [];

    public record Option
    {
        public ProjectId Id { get; set; }
        public string Name { get; set; }
    }
}

/// Active (non-archived) projects, ordered by name. Feeds every project dropdown.
public class GetProjectOptionsHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetProjectOptionsQuery, Result<GetProjectOptionsResult>>
{
    public async ValueTask<Result<GetProjectOptionsResult>> Handle(
        GetProjectOptionsQuery request, CancellationToken cancellationToken)
    {
        var projects = await readDb.Projects
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.Name)
            .Select(x => new GetProjectOptionsResult.Option { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

        return Result<GetProjectOptionsResult>.OK(new GetProjectOptionsResult { Projects = projects });
    }
}

public record GetUnreleasedScheduledNoticesQuery : IRequest<Result<GetUnreleasedScheduledNoticesResult>>
{
    public ProjectId ProjectId { get; set; }
}

public record GetUnreleasedScheduledNoticesResult
{
    public IReadOnlyList<Option> Notices { get; set; } = [];

    public record Option
    {
        public ReleaseNoticeId Id { get; set; }
        public string Version { get; set; }
        public DateTime ScheduledFor { get; set; }
        public string Summary { get; set; }
        public string LeadDeveloper { get; set; }

        /// A computed property on the DTO so the dropdown can bind TextProperty="Display".
        public string Display => $"{Version} - {ScheduledFor:dd/MM/yyyy HH:mm} - {Shorten(Summary)}";

        private static string Shorten(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "(no summary)";
            var single = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return single.Length <= 60 ? single : single.Substring(0, 60) + "...";
        }
    }
}

/// Scheduled notices for that project NOT YET LINKED to any LiveRelease, newest first.
public class GetUnreleasedScheduledNoticesHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetUnreleasedScheduledNoticesQuery, Result<GetUnreleasedScheduledNoticesResult>>
{
    public async ValueTask<Result<GetUnreleasedScheduledNoticesResult>> Handle(
        GetUnreleasedScheduledNoticesQuery request, CancellationToken cancellationToken)
    {
        var linkedIds = readDb.LiveReleases
            .Where(x => x.ScheduledReleaseId != null)
            .Select(x => x.ScheduledReleaseId);   // the Where above guarantees this is non-null

        var notices = await readDb.ScheduledReleases
            .Where(x => x.ProjectId == request.ProjectId && !linkedIds.Contains(x.Id))
            .OrderByDescending(x => x.ScheduledFor)
            .Select(x => new GetUnreleasedScheduledNoticesResult.Option
            {
                Id = x.Id,
                Version = x.Version,
                ScheduledFor = x.ScheduledFor,
                Summary = x.Summary,
                LeadDeveloper = x.LeadDeveloper
            })
            .ToListAsync(cancellationToken);

        return Result<GetUnreleasedScheduledNoticesResult>.OK(
            new GetUnreleasedScheduledNoticesResult { Notices = notices });
    }
}

public record GetRollbackCandidatesQuery : IRequest<Result<GetRollbackCandidatesResult>>
{
    /// Null means every project.
    public ProjectId? ProjectId { get; set; }
}

public record GetRollbackCandidatesResult
{
    public IReadOnlyList<Candidate> Candidates { get; set; } = [];

    public record Candidate
    {
        public ReleaseNoticeId Id { get; set; }
        public ProjectId ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Version { get; set; }
        public DateTime DeployedOn { get; set; }
        public string LeadDeveloper { get; set; }

        public string Display => $"{ProjectName} {Version} - deployed {DeployedOn:dd/MM/yyyy HH:mm}";
    }
}

/// Live releases NOT ALREADY ROLLED BACK, newest deployment first, optionally filtered by project.
public class GetRollbackCandidatesHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetRollbackCandidatesQuery, Result<GetRollbackCandidatesResult>>
{
    public async ValueTask<Result<GetRollbackCandidatesResult>> Handle(
        GetRollbackCandidatesQuery request, CancellationToken cancellationToken)
    {
        var rolledBackIds = readDb.Rollbacks.Select(x => x.RollbackOfReleaseId);

        var query = readDb.LiveReleases
            .Where(x => !rolledBackIds.Contains(x.Id));

        if (request.ProjectId is not null)
            query = query.Where(x => x.ProjectId == request.ProjectId);

        var candidates = await query
            .OrderByDescending(x => x.DeployedOn)
            .Select(x => new GetRollbackCandidatesResult.Candidate
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                ProjectName = readDb.Projects.Where(p => p.Id == x.ProjectId).Select(p => p.Name).FirstOrDefault(),
                Version = x.Version,
                DeployedOn = x.DeployedOn,
                LeadDeveloper = x.LeadDeveloper
            })
            .ToListAsync(cancellationToken);

        return Result<GetRollbackCandidatesResult>.OK(
            new GetRollbackCandidatesResult { Candidates = candidates });
    }
}
