namespace DemoReleaseChatPlugin.Assistant;

/// What the current user may reach through the assistant, computed per user from the app's own
/// authorization. The chat is never a way around the menu: a tool for data the user can't open on a
/// page is never offered to the model.
public sealed record AssistantCapabilities(bool CanAccessReleases)
{
    public static readonly AssistantCapabilities None = new(false);
}
