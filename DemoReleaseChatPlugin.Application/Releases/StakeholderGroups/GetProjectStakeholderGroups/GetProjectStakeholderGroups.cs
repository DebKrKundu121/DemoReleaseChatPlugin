using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.GetProjectStakeholderGroups;

public record GetProjectStakeholderGroupsQuery : IRequest<QueryResult<GetProjectStakeholderGroupsResult>>
{
    public ProjectId ProjectId { get; set; }
    public bool IncludeInactive { get; set; }
}

/// Carries the project header the Manage Project page needs, so one round trip fills the whole page.
public record GetProjectStakeholderGroupsResult
{
    public ProjectId ProjectId { get; set; }
    public string ProjectName { get; set; }
    public string ProjectDescription { get; set; }
    public bool IsProjectArchived { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; }
    public int ReleaseCount { get; set; }

    /// Surfaced separately so the full audience is visible in one place - the project's own page
    /// only shows its own groups.
    public int ActiveMandatoryStakeholderCount { get; set; }

    public IReadOnlyList<StakeholderGroupDto> Groups { get; set; } = [];

    public int ActiveGroupCount => Groups.Count(x => x.IsActive);
    public int ActiveStakeholderCount => Groups.Where(x => x.IsActive).Sum(x => x.ActiveStakeholderCount);
}

public class GetProjectStakeholderGroupsValidator : AbstractValidator<GetProjectStakeholderGroupsQuery>
{
    public GetProjectStakeholderGroupsValidator() => RuleFor(x => x.ProjectId).NotNull();
}

public class GetProjectStakeholderGroupsHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetProjectStakeholderGroupsQuery, QueryResult<GetProjectStakeholderGroupsResult>>
{
    public async ValueTask<QueryResult<GetProjectStakeholderGroupsResult>> Handle(
        GetProjectStakeholderGroupsQuery request, CancellationToken cancellationToken)
    {
        var project = await readDb.Projects
            .FirstOrDefaultAsync(x => x.Id == request.ProjectId, cancellationToken);

        if (project is null)
            return QueryResult<GetProjectStakeholderGroupsResult>.NotFound("Project not found.");

        var mandatoryCount = await readDb.Stakeholders
            .CountAsync(
                x => x.IsMandatory
                     && x.IsActive
                     && readDb.StakeholderGroups.Any(g => g.Id == x.GroupId && g.IsActive),
                cancellationToken);

        var groupsQuery = readDb.StakeholderGroups
            .Where(x => x.ProjectId == request.ProjectId);

        if (!request.IncludeInactive)
            groupsQuery = groupsQuery.Where(x => x.IsActive);

        var groups = await groupsQuery
            .OrderBy(x => x.Name)
            .Select(x => new StakeholderGroupDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                IsMandatory = x.IsMandatory,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,
                Stakeholders = readDb.Stakeholders
                    .Where(s => s.GroupId == x.Id && (request.IncludeInactive || s.IsActive))
                    .OrderBy(s => s.Name)
                    .Select(s => new GroupStakeholderDto
                    {
                        Id = s.Id,
                        GroupId = s.GroupId,
                        Name = s.Name,
                        Email = s.Email,
                        IsActive = s.IsActive,
                        CreatedOn = s.CreatedOn,
                        CreatedBy = s.CreatedBy
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var releaseCount = await readDb.ReleaseNotices
            .CountAsync(x => x.ProjectId == request.ProjectId, cancellationToken);

        return QueryResult<GetProjectStakeholderGroupsResult>.Of(new GetProjectStakeholderGroupsResult
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectDescription = project.Description,
            IsProjectArchived = project.IsArchived,
            CreatedOn = project.CreatedOn,
            CreatedBy = project.CreatedBy,
            ReleaseCount = releaseCount,
            ActiveMandatoryStakeholderCount = mandatoryCount,
            Groups = groups
        });
    }
}
