namespace DemoReleaseChatPlugin.Assistant.Tools;

/// Tool names as the model sees them. Constants, never literals, because several places must agree:
/// the registry, the activity labels, and the panel's checks for particular tools.
public static class ToolNames
{
    public const string GetCurrentDateTime = "get_current_datetime";
    public const string ListProjects = "list_projects";
    public const string GetReleaseHistory = "get_release_history";
    public const string GetLatestRelease = "get_latest_release";
    public const string StartNewReport = "start_new_report";
    public const string PresentTable = "present_table";

    /// What the user sees while a tool runs, and on the label under the answer.
    public static string DisplayLabel(string toolName) => toolName switch
    {
        GetCurrentDateTime => "Checking the date…",
        ListProjects => "Listing projects…",
        GetReleaseHistory => "Checking release history…",
        GetLatestRelease => "Checking the latest release…",
        StartNewReport => "Starting a new report…",
        PresentTable => "Preparing the table…",
        _ => "Looking things up…"
    };
}
