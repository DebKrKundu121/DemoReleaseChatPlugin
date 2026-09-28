using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.StakeholderGroups.UpdateStakeholderGroup;

public record UpdateStakeholderGroupCommand : IRequest<Result>
{
    public StakeholderGroupId GroupId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

public record GetUpdateStakeholderGroupQuery : IRequest<QueryResult<GetUpdateStakeholderGroupResult>>
{
    public StakeholderGroupId GroupId { get; set; }
}

public record GetUpdateStakeholderGroupResult
{
    public UpdateStakeholderGroupCommand Command { get; set; }
    public bool IsActive { get; set; }
    public bool IsMandatory { get; set; }
    public ProjectId? ProjectId { get; set; }
}

public class UpdateStakeholderGroupValidator : AbstractValidator<UpdateStakeholderGroupCommand>
{
    private readonly IDemoReleaseChatPluginReadDb _readDb;

    public UpdateStakeholderGroupValidator(IDemoReleaseChatPluginReadDb readDb)
    {
        _readDb = readDb;

        RuleFor(x => x.GroupId).NotNull();

        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).MustAsync(BeUniqueName)
            .WithMessage("A stakeholder group with this name already exists here.");

        RuleFor(x => x.Description).MaximumLength(500);
    }

    private async Task<bool> BeUniqueName(
        UpdateStakeholderGroupCommand command, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;

        var group = await _readDb.StakeholderGroups
            .FirstOrDefaultAsync(x => x.Id == command.GroupId, cancellationToken);

        // Leave the "not found" case to the handler's Result.Error.
        if (group is null) return true;

        var candidate = name.Trim();

        return !await _readDb.StakeholderGroups
            .AnyAsync(
                x => x.ProjectId == group.ProjectId && x.Name == candidate && x.Id != command.GroupId,
                cancellationToken);
    }
}

public class GetUpdateStakeholderGroupValidator : AbstractValidator<GetUpdateStakeholderGroupQuery>
{
    public GetUpdateStakeholderGroupValidator() => RuleFor(x => x.GroupId).NotNull();
}

public class GetUpdateStakeholderGroupHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetUpdateStakeholderGroupQuery, QueryResult<GetUpdateStakeholderGroupResult>>
{
    public async ValueTask<QueryResult<GetUpdateStakeholderGroupResult>> Handle(
        GetUpdateStakeholderGroupQuery request, CancellationToken cancellationToken)
    {
        var group = await readDb.StakeholderGroups
            .Where(x => x.Id == request.GroupId)
            .Select(x => new GetUpdateStakeholderGroupResult
            {
                IsActive = x.IsActive,
                IsMandatory = x.IsMandatory,
                ProjectId = x.ProjectId,
                Command = new UpdateStakeholderGroupCommand
                {
                    GroupId = x.Id,
                    Name = x.Name,
                    Description = x.Description
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        return group is null
            ? QueryResult<GetUpdateStakeholderGroupResult>.NotFound("Stakeholder group not found.")
            : QueryResult<GetUpdateStakeholderGroupResult>.Of(group);
    }
}

public class UpdateStakeholderGroupHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStakeholderGroupCommand, Result>
{
    public async ValueTask<Result> Handle(
        UpdateStakeholderGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await dbContext.StakeholderGroups
            .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (group is null)
            return Result.Error("Stakeholder group not found.");

        var actor = await currentUser.ProvideAsync();

        var result = group.UpdateDetails(request.Name, request.Description, actor, clock);
        if (result.IsFailure) return result;

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.OK();
    }
}
