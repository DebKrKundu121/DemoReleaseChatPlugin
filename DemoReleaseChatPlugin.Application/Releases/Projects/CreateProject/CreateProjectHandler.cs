using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using Mediator;

namespace DemoReleaseChatPlugin.Application.Releases.Projects.CreateProject;

/// Load, invoke, save. The id and the clock are created here, in the application layer, and handed
/// to the domain - which is what keeps Project itself free of DateTime.Now and Guid.NewGuid().
public class CreateProjectHandler(
    IDemoReleaseChatPluginDbContext dbContext,
    ICurrentUserProvider currentUser,
    IClock clock,
    IDemoReleaseChatPluginUnitOfWork unitOfWork)
    : IRequestHandler<CreateProjectCommand, Result<ProjectId>>
{
    public async ValueTask<Result<ProjectId>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var actor = await currentUser.ProvideAsync();

        var project = Project.Create(ProjectId.New(), request.Name, request.Description, actor, clock);

        dbContext.Projects.Add(project);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result<ProjectId>.OK(project.Id);
    }
}
