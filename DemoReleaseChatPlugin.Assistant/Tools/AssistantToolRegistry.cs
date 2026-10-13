using Microsoft.Extensions.AI;

namespace DemoReleaseChatPlugin.Assistant.Tools;

/// Builds the tools offered to the model. Called fresh on every turn, so a capability change takes
/// effect immediately. The gate is omission, not refusal: a tool the user may not use is never
/// offered, so there is nothing to talk the model into.
public sealed class AssistantToolRegistry(SystemTools systemTools, PresentationTools presentationTools, ReleaseTools releaseTools)
{
    public IReadOnlyList<AITool> BuildTools(AssistantCapabilities capabilities)
    {
        // AIFunctionFactory derives the JSON schema the model sees from the method's signature and
        // [Description] attributes.

        // Always available: they reach no protected data of their own.
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(systemTools.GetCurrentDateTime,      ToolNames.GetCurrentDateTime),
            AIFunctionFactory.Create(presentationTools.StartNewReport,    ToolNames.StartNewReport),
            AIFunctionFactory.Create(presentationTools.PresentTable,      ToolNames.PresentTable)
        };

        if (capabilities.CanAccessReleases)
        {
            tools.Add(AIFunctionFactory.Create(releaseTools.ListProjects,      ToolNames.ListProjects));
            tools.Add(AIFunctionFactory.Create(releaseTools.GetReleaseHistory, ToolNames.GetReleaseHistory));
            tools.Add(AIFunctionFactory.Create(releaseTools.GetLatestRelease,  ToolNames.GetLatestRelease));
        }

        return tools;
    }
}
