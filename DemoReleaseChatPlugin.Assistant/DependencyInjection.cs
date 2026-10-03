using DemoReleaseChatPlugin.Assistant.Chat;
using DemoReleaseChatPlugin.Assistant.Prompts;
using Microsoft.Extensions.DependencyInjection;

namespace DemoReleaseChatPlugin.Assistant;

public static class DependencyInjection
{
    /// The provider-agnostic assistant. The host must additionally register an IChatClient (the
    /// Website does, in AssistantDependencyInjection) and an IClock (Infrastructure does).
    public static IServiceCollection AddAssistantCore(this IServiceCollection services)
    {
        services.AddSingleton<SystemPromptBuilder>();

        // Scoped = one per Blazor circuit, i.e. one conversation per browser tab.
        services.AddScoped<AssistantChatSession>();

        return services;
    }
}
