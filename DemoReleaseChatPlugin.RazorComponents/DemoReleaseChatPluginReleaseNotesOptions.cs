namespace DemoReleaseChatPlugin.RazorComponents;

public sealed class DemoReleaseChatPluginReleaseNotesOptions
{
    public string ApplicationName { get; set; } = "";
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public string? ApiBaseUrl { get; set; }
}
