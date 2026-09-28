using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Projects.GetProjects;

public record GetProjectsQuery : IRequest<Result<GetProjectsResult>>
{
    public bool IncludeArchived { get; set; }
}

public record GetProjectsResult
{
    public IReadOnlyList<ProjectDto> Projects { get; set; } = [];

    public record ProjectDto
    {
        public ProjectId Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsArchived { get; set; }

        public int ProjectStakeholderCount { get; set; }
        public int MandatoryStakeholderCount { get; set; }
        public int GroupCount { get; set; }
        public int ReleaseCount { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }

        public int StakeholderCount => MandatoryStakeholderCount + ProjectStakeholderCount;

        public string StakeholderBreakdown =>
            $"{MandatoryStakeholderCount} mandatory + {ProjectStakeholderCount} on this project";
    }
}

public class GetProjectsHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetProjectsQuery, Result<GetProjectsResult>>
{
    public async ValueTask<Result<GetProjectsResult>> Handle(
        GetProjectsQuery request, CancellationToken cancellationToken)
    {
        // The same number for every row, so it is computed once outside the per-project projection.
        var mandatoryStakeholderCount = await readDb.Stakeholders
            .CountAsync(
                x => x.IsMandatory
                     && x.IsActive
                     && readDb.StakeholderGroups.Any(g => g.Id == x.GroupId && g.IsActive),
                cancellationToken);

        var query = readDb.Projects;

        if (!request.IncludeArchived)
            query = query.Where(x => !x.IsArchived);

        // Counts only include ACTIVE stakeholders in ACTIVE groups.
        var projects = await query
            .OrderBy(x => x.Name)
            .Select(x => new GetProjectsResult.ProjectDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsArchived = x.IsArchived,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,
                MandatoryStakeholderCount = mandatoryStakeholderCount,
                GroupCount = readDb.StakeholderGroups.Count(g => g.ProjectId == x.Id && g.IsActive),
                ProjectStakeholderCount = readDb.Stakeholders.Count(
                    s => s.ProjectId == x.Id
                         && s.IsActive
                         && readDb.StakeholderGroups.Any(g => g.Id == s.GroupId && g.IsActive)),
                ReleaseCount = readDb.ReleaseNotices.Count(n => n.ProjectId == x.Id)
            })
            .ToListAsync(cancellationToken);

        return Result<GetProjectsResult>.OK(new GetProjectsResult { Projects = projects });
    }
}
