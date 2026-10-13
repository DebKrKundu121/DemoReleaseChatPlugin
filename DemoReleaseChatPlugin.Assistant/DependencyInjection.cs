using DemoReleaseChatPlugin.Assistant.Canvas;
using DemoReleaseChatPlugin.Assistant.Chat;
using DemoReleaseChatPlugin.Assistant.Prompts;
using DemoReleaseChatPlugin.Assistant.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DemoReleaseChatPlugin.Assistant;

public static class DependencyInjection
{
    /// The provider-agnostic assistant. The host must additionally register an IChatClient and an
    /// IAssistantWorkspace (the page that displays presented results).
    public static IServiceCollection AddAssistantCore(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<SystemPromptBuilder>();

        // The real tables behind the workspace's Verified chip — one store per circuit.
        services.AddScoped<IWorkspaceDataStore, WorkspaceDataStore>();

        // Tools — scoped, one per circuit, like the session that uses them.
        services.AddScoped<SystemTools>();
        services.AddScoped<PresentationTools>();
        services.AddScoped<ReleaseTools>();
        services.AddScoped<AssistantToolRegistry>();

        // One conversation per circuit (browser tab).
        services.AddScoped<AssistantChatSession>();

        return services;
    }
}
