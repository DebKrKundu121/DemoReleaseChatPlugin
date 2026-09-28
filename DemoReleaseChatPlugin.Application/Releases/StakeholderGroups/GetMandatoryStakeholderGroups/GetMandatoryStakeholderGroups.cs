using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DoverPort.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.GetMandatoryStakeholderGroups;

public record GetMandatoryStakeholderGroupsQuery : IRequest<Result<GetMandatoryStakeholderGroupsResult>>
{
    public bool IncludeInactive { get; set; }
}

public record GetMandatoryStakeholderGroupsResult
{
    public IReadOnlyList<StakeholderGroupDto> Groups { get; set; } = [];

    public int ActiveGroupCount => Groups.Count(x => x.IsActive);
    public int ActiveStakeholderCount => Groups.Where(x => x.IsActive).Sum(x => x.ActiveStakeholderCount);
}

public class GetMandatoryStakeholderGroupsHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetMandatoryStakeholderGroupsQuery, Result<GetMandatoryStakeholderGroupsResult>>
{
    public async ValueTask<Result<GetMandatoryStakeholderGroupsResult>> Handle(
        GetMandatoryStakeholderGroupsQuery request, CancellationToken cancellationToken)
    {
        var query = readDb.StakeholderGroups
            .Where(x => x.IsMandatory);

        if (!request.IncludeInactive)
            query = query.Where(x => x.IsActive);

        var groups = await query
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

        return Result<GetMandatoryStakeholderGroupsResult>.OK(
            new GetMandatoryStakeholderGroupsResult { Groups = groups });
    }
}
