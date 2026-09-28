using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.GetStakeholderGroup;

public record GetStakeholderGroupQuery : IRequest<QueryResult<GetStakeholderGroupResult>>
{
    public StakeholderGroupId GroupId { get; set; }
}

public record GetStakeholderGroupResult
{
    public StakeholderGroupDto Group { get; set; }
}

public class GetStakeholderGroupValidator : AbstractValidator<GetStakeholderGroupQuery>
{
    public GetStakeholderGroupValidator() => RuleFor(x => x.GroupId).NotNull();
}

/// <summary>
/// NEVER filters by IsActive - the manage dialog must show inactive people so they can be
/// reactivated. The two list queries do filter, via IncludeInactive.
/// </summary>
public class GetStakeholderGroupHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetStakeholderGroupQuery, QueryResult<GetStakeholderGroupResult>>
{
    public async ValueTask<QueryResult<GetStakeholderGroupResult>> Handle(
        GetStakeholderGroupQuery request, CancellationToken cancellationToken)
    {
        var group = await readDb.StakeholderGroups
            .Where(x => x.Id == request.GroupId)
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
                    .Where(s => s.GroupId == x.Id)
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
            .FirstOrDefaultAsync(cancellationToken);

        return group is null
            ? QueryResult<GetStakeholderGroupResult>.NotFound("Stakeholder group not found.")
            : QueryResult<GetStakeholderGroupResult>.Of(new GetStakeholderGroupResult { Group = group });
    }
}
