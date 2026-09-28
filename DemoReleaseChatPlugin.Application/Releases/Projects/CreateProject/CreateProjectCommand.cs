using DemoReleaseChatPlugin.Domain.Releases.Values;
using DoverPort.Common.Models;
using Mediator;

namespace DemoReleaseChatPlugin.Application.Releases.Projects.CreateProject;

public record CreateProjectCommand : IRequest<Result<ProjectId>>
{
    public string Name { get; set; }
    public string Description { get; set; }
}
