using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Projects.UpdateProject;

public record UpdateProjectCommand : IRequest<Result>
{
    public ProjectId ProjectId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// The "load a command, edit it, send it back" pattern: this query returns the COMMAND OBJECT
/// itself, pre-filled. The page binds straight to it and posts it back.
/// Same shape in UpdateStakeholderGroup, UpdateStakeholder and UpdateUserRoles.
/// </summary>
public record GetUpdateProjectQuery : IRequest<QueryResult<GetUpdateProjectResult>>
{
    public ProjectId ProjectId { get; set; }
}

public record GetUpdateProjectResult
{
    public UpdateProjectCommand Command { get; set; }
    public bool IsArchived { get; set; }
}

public class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    private readonly IDemoReleaseChatPluginReadDb _readDb;

    public UpdateProjectValidator(IDemoReleaseChatPluginReadDb readDb)
    {
        _readDb = readDb;

        RuleFor(x => x.ProjectId).NotNull();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .MustAsync(BeUniqueName)
            .WithMessage("A project with this name already exists.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);
    }

    /// Excludes the row being edited, so re-saving without renaming is not a clash.
    private async Task<bool> BeUniqueName(
        UpdateProjectCommand command, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;

        var candidate = name.Trim();

        return !await _readDb.Projects
            .AnyAsync(x => x.Name == candidate && x.Id != command.ProjectId, cancellationToken);
    }
}

public class GetUpdateProjectValidator : AbstractValidator<GetUpdateProjectQuery>
{
    public GetUpdateProjectValidator() => RuleFor(x => x.ProjectId).NotNull();
}

public class GetUpdateProjectHandler(IDemoReleaseChatPluginReadDb readDb)
    : IRequestHandler<GetUpdateProjectQuery, QueryResult<GetUpdateProjectResult>>
{
    public async ValueTask<QueryResult<GetUpdateProjectResult>> Handle(
        GetUpdateProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await readDb.Projects
            .Where(x => x.Id == request.ProjectId)
            .Select(x => new GetUpdateProjectResult
            {
                IsArchived = x.IsArchived,
                Command = new UpdateProjectCommand
                {
                    ProjectId = x.Id,
                    Name = x.Name,
                    Description = x.Description
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        return project is null
            ? QueryResult<GetUpdateProjectResult>.NotFound("Project not found.")
            : QueryResult<GetUpdateProjectResult>.Of(project);
    }
}

/// Load, invoke, save. The rule "an archived project cannot be edited" is NOT here - it is inside
/// Project.UpdateDetails, so no other caller can bypass it. This handler only relays the answer.
public class UpdateProjectHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProjectCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(x => x.Id == request.ProjectId, cancellationToken);

        if (project is null)
            return Result.Error("Project not found.");

        var actor = await currentUser.ProvideAsync();

        var result = project.UpdateDetails(request.Name, request.Description, actor, clock);

        if (result.IsFailure) return result;

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.OK();
    }
}
