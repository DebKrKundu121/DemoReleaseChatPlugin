using DemoReleaseChatPlugin.Assistant.Chat;
using DemoReleaseChatPlugin.Assistant.Prompts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DemoReleaseChatPlugin.Assistant;

public static class DependencyInjection
{
    /// The provider-agnostic assistant. The host must additionally register an IChatClient.
    public static IServiceCollection AddAssistantCore(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<SystemPromptBuilder>();

        // One conversation per circuit (browser tab).
        services.AddScoped<AssistantChatSession>();

        return services;
    }
}
