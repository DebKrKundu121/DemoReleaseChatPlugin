using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Notices.GetReleaseHistory;

public record GetReleaseHistoryQuery : IRequest<Result<GetReleaseHistoryResult>>
{
    /// Null means every project.
    public ProjectId? ProjectId { get; set; }
    public string Kind { get; set; }
}

public record GetReleaseHistoryResult
{
    public IReadOnlyList<NoticeDto> Notices { get; set; } = [];

    /// <summary>
    /// Shaped for the grid, not for the domain: <c>Kind</c> is the plain string the kind filter
    /// binds to, while <c>RiskLevel</c> keeps its vocabulary type because the badge renders it
    /// directly and "no risk declared" has to stay distinguishable from "Low".
    /// </summary>
    public record NoticeDto
    {
        public ReleaseNoticeId Id { get; set; }
        public string Kind { get; set; }
        public ProjectId ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Version { get; set; }
        public string LeadDeveloper { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }

        /// ScheduledFor | DeployedOn | RolledBackOn
        public DateTime EffectiveOn { get; set; }

        /// Summary | ReleaseNotes | RollbackReason
        public string Detail { get; set; }

        public RiskLevel RiskLevel { get; set; }   // Scheduled only
        public string RiskNotes { get; set; }      // Scheduled only
        public bool IsRolledBack { get; set; }     // Live only
        public bool WasAnnounced { get; set; }     // Live only: ScheduledReleaseId != null
    }
}

/// <summary>
/// Runs up to three separate TYPED queries and concatenates, rather than one query over the base
/// set - the three kinds project different columns into EffectiveOn and Detail.
/// <para>
/// The project name comes from a correlated sub-select rather than a <c>Project</c> navigation
/// property, which no longer exists: a release notice and a project are separate aggregates, and a
/// read is allowed to join them even though the write model is not allowed to hold a reference.
/// It is still one SQL statement per kind.
/// </para>
/// </summary>
public class GetReleaseHistoryHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetReleaseHistoryQuery, Result<GetReleaseHistoryResult>>
{
    public async ValueTask<Result<GetReleaseHistoryResult>> Handle(
        GetReleaseHistoryQuery request, CancellationToken cancellationToken)
    {
        var notices = new List<GetReleaseHistoryResult.NoticeDto>();

        var wantsAll = string.IsNullOrWhiteSpace(request.Kind);

        if (wantsAll || request.Kind == ReleaseNoticeKind.Scheduled.Value)
            notices.AddRange(await ScheduledAsync(request.ProjectId, cancellationToken));

        if (wantsAll || request.Kind == ReleaseNoticeKind.Live.Value)
            notices.AddRange(await LiveAsync(request.ProjectId, cancellationToken));

        if (wantsAll || request.Kind == ReleaseNoticeKind.Rollback.Value)
            notices.AddRange(await RollbacksAsync(request.ProjectId, cancellationToken));

        // Ordered by when it was FILED, not by EffectiveOn. The grid sorts by EffectiveOn
        // client-side by default; the underlying list is filing order.
        var ordered = notices.OrderByDescending(x => x.CreatedOn).ToList();

        return Result<GetReleaseHistoryResult>.OK(new GetReleaseHistoryResult { Notices = ordered });
    }

    private async Task<List<GetReleaseHistoryResult.NoticeDto>> ScheduledAsync(
        ProjectId? projectId, CancellationToken cancellationToken)
    {
        var kind = ReleaseNoticeKind.Scheduled.Value;
        var query = readDb.ScheduledReleases;

        if (projectId is not null)
            query = query.Where(x => x.ProjectId == projectId);

        return await query
            .Select(x => new GetReleaseHistoryResult.NoticeDto
            {
                Id = x.Id,
                Kind = kind,
                ProjectId = x.ProjectId,
                ProjectName = readDb.Projects.Where(p => p.Id == x.ProjectId).Select(p => p.Name).FirstOrDefault(),
                Version = x.Version,
                LeadDeveloper = x.LeadDeveloper,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,
                EffectiveOn = x.ScheduledFor,
                Detail = x.Summary,
                RiskLevel = x.RiskLevel,
                RiskNotes = x.RiskNotes
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<GetReleaseHistoryResult.NoticeDto>> LiveAsync(
        ProjectId? projectId, CancellationToken cancellationToken)
    {
        var kind = ReleaseNoticeKind.Live.Value;
        var query = readDb.LiveReleases;

        if (projectId is not null)
            query = query.Where(x => x.ProjectId == projectId);

        // IsRolledBack is projected INSIDE the query, so this is one SQL statement, not N+1.
        return await query
            .Select(x => new GetReleaseHistoryResult.NoticeDto
            {
                Id = x.Id,
                Kind = kind,
                ProjectId = x.ProjectId,
                ProjectName = readDb.Projects.Where(p => p.Id == x.ProjectId).Select(p => p.Name).FirstOrDefault(),
                Version = x.Version,
                LeadDeveloper = x.LeadDeveloper,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,
                EffectiveOn = x.DeployedOn,
                Detail = x.ReleaseNotes,
                IsRolledBack = readDb.Rollbacks.Select(r => r.RollbackOfReleaseId).Contains(x.Id),
                WasAnnounced = x.ScheduledReleaseId != null
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<GetReleaseHistoryResult.NoticeDto>> RollbacksAsync(
        ProjectId? projectId, CancellationToken cancellationToken)
    {
        var kind = ReleaseNoticeKind.Rollback.Value;
        var query = readDb.Rollbacks;

        if (projectId is not null)
            query = query.Where(x => x.ProjectId == projectId);

        return await query
            .Select(x => new GetReleaseHistoryResult.NoticeDto
            {
                Id = x.Id,
                Kind = kind,
                ProjectId = x.ProjectId,
                ProjectName = readDb.Projects.Where(p => p.Id == x.ProjectId).Select(p => p.Name).FirstOrDefault(),
                Version = x.Version,
                LeadDeveloper = x.LeadDeveloper,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,
                EffectiveOn = x.RolledBackOn,
                Detail = x.RollbackReason
            })
            .ToListAsync(cancellationToken);
    }
}
