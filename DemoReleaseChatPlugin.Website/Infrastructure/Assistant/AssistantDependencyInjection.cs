using System;
using Azure;
using Azure.AI.OpenAI;
using DemoReleaseChatPlugin.Assistant;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DemoReleaseChatPlugin.Website.Infrastructure.Assistant
{
    public static class AssistantDependencyInjection
    {
        /// Wires up the AI assistant. Azure OpenAI is bound here and nowhere else — the Assistant
        /// project only knows Microsoft.Extensions.AI's IChatClient, so swapping provider is a change
        /// to this file alone.
        public static void AddAssistant(this IHostApplicationBuilder builder)
        {
            var assistantSection = builder.Configuration.GetSection(AssistantOptions.SectionName);
            var assistant = assistantSection.Get<AssistantOptions>() ?? new AssistantOptions();

            builder.Services.AddOptions<AssistantOptions>()
                .Bind(assistantSection)
                .ValidateOnStart();

            var azure = builder.Services.AddOptions<AzureOpenAiOptions>()
                .Bind(builder.Configuration.GetSection(AzureOpenAiOptions.SectionName));

            if (!assistant.Enabled)
            {
                return;                       // ← everything below is the model half
            }

            // Validated only when switched on: failing startup over settings nothing reads would take
            // the whole app down.
            azure
                .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint),       "Assistant: AzureOpenAI:Endpoint is required when Assistant:Enabled is true")
                .Validate(o => IsBaseUrl(o.Endpoint),                        "Assistant: AzureOpenAI:Endpoint must be the resource's base URL, e.g. https://name.openai.azure.com/ (not the /openai/v1 address)")
                .Validate(o => !string.IsNullOrWhiteSpace(o.DeploymentName), "Assistant: AzureOpenAI:DeploymentName is required when Assistant:Enabled is true")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey),         "Assistant: AzureOpenAI:ApiKey is required when Assistant:Enabled is true")
                .ValidateOnStart();

            builder.Services.AddChatClient(sp =>
                {
                    var options = sp.GetRequiredService<IOptions<AzureOpenAiOptions>>().Value;

                    return new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey))
                        .GetChatClient(options.DeploymentName)
                        .AsIChatClient();
                })
                // Runs the tool loop: calls the tools the model asks for, feeds the results back, and
                // repeats up to the cap. The chat session never dispatches a tool call itself.
                .UseFunctionInvocation(configure: client =>
                {
                    client.MaximumIterationsPerRequest = assistant.MaxToolIterationsPerTurn;
                })
                .UseLogging();
        }

        /// The SDK appends /openai/deployments/... itself, so the endpoint must be the bare resource
        /// URL. Pasting the portal's /openai/v1 address instead makes every call fail.
        private static bool IsBaseUrl(string endpoint)
            => Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
               && uri.Scheme == Uri.UriSchemeHttps
               && uri.AbsolutePath == "/";
    }
}
