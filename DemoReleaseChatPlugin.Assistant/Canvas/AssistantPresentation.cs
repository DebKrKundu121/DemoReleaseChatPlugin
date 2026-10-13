namespace DemoReleaseChatPlugin.Assistant.Canvas;

/// The kinds of thing the workspace can show. Deliberately closed: the model picks a kind and supplies
/// typed data, it never emits markup. Chart, List, TaskList and Summary arrive with their tools.
public enum PresentationKind { Table }

/// One component of a workspace report. Immutable once created; the payload matching Kind is set.
public sealed class AssistantPresentation
{
    public required string Title { get; init; }
    public required PresentationKind Kind { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public TablePayload Table { get; init; }
}

public sealed class TablePayload
{
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }

    /// True when the rows came straight from a tool's result (via a source token).
    /// False when the model typed them itself — shown with a stronger caveat.
    public bool Verified { get; init; }
}
