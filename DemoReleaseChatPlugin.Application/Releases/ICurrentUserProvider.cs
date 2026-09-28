using DemoReleaseChatPlugin.Domain.Releases;

namespace DemoReleaseChatPlugin.Application.Releases;

public interface ICurrentUserProvider
{
    Task<Actor> ProvideAsync();
}
