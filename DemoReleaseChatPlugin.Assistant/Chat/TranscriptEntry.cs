namespace DemoReleaseChatPlugin.Assistant.Chat;

public enum TranscriptRole { User, Assistant }

/// One message as the UI shows it. The transcript is never trimmed, unlike the model's history.
public class TranscriptEntry
{
    public TranscriptRole Role { get; init; }

    /// Mutable: an assistant reply is added empty and grows as the stream arrives.
    public string Text { get; set; }

    public bool IsError { get; set; }
    public DateTimeOffset Timestamp { get; init; }
    public string ConversationId { get; init; }
    public int TurnNumber { get; init; }

    /// The tools called while producing this reply, in order.
    public List<ToolActivity> ToolActivities { get; } = new();
}

public class ToolActivity
{
    /// The raw name, e.g. "get_current_datetime" — the label's tooltip, for diagnosing answers.
    public string ToolName { get; init; }

    /// The friendly text, e.g. "Checking the date…" — what the user sees.
    public string DisplayLabel { get; init; }
}
