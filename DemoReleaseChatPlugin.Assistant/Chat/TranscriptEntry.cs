namespace DemoReleaseChatPlugin.Assistant.Chat;

public enum TranscriptRole
{
    User,
    Assistant
}

/// One message as the chat panel shows it. The transcript is the UI's record of the conversation and
/// is never trimmed; what the model sees is the session's separate, trimmed message history.
public class TranscriptEntry
{
    public TranscriptRole Role { get; init; }

    /// Mutable: an assistant entry is added empty and filled in when the reply arrives.
    public string Text { get; set; }

    public bool IsError { get; set; }
    public DateTime Timestamp { get; init; }
    public string ConversationId { get; init; }
    public int TurnNumber { get; init; }
}
