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
}
