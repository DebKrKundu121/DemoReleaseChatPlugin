using Microsoft.Extensions.AI;

namespace DemoReleaseChatPlugin.Assistant.Tools;

/// Builds the tools offered to the model. Called fresh on every turn, so when tools become gated by
/// what the user may access, a change takes effect immediately. The gate is omission, not refusal:
/// a tool the user may not use is never offered, so there is nothing to talk the model into.
public sealed class AssistantToolRegistry(SystemTools systemTools)
{
    public IReadOnlyList<AITool> BuildTools()
    {
        // AIFunctionFactory derives the JSON schema the model sees from the method's signature and
        // [Description] attributes.
        return new List<AITool>
        {
            AIFunctionFactory.Create(systemTools.GetCurrentDateTime, ToolNames.GetCurrentDateTime)
        };
    }
}
