namespace DemoReleaseChatPlugin.Assistant.Tools;

/// Tool names as the model sees them. Constants, never literals, because several places must agree:
/// the registry, the activity labels, and the panel's checks for particular tools.
public static class ToolNames
{
    public const string GetCurrentDateTime = "get_current_datetime";

    /// What the user sees while a tool runs, and on the label under the answer.
    public static string DisplayLabel(string toolName) => toolName switch
    {
        GetCurrentDateTime => "Checking the date…",
        _ => "Looking things up…"
    };
}
