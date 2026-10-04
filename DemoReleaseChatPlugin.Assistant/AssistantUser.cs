namespace DemoReleaseChatPlugin.Assistant;

/// Who the assistant is talking to. Id is the stable key for anything scoped to a person (two people
/// can share a display name, and names change) and is never told to the model.
public sealed record AssistantUser(string Id, string DisplayName, IReadOnlyList<string> Roles)
{
    public static readonly AssistantUser Unknown = new(null, null, Array.Empty<string>());

    public AssistantUser(string id, string displayName) : this(id, displayName, Array.Empty<string>()) { }

    public bool IsKnown => !string.IsNullOrWhiteSpace(DisplayName);
}
