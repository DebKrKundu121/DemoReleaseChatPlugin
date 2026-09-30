namespace DemoReleaseChatPlugin.Assistant;

/// Behaviour settings for the assistant, bound from the "Assistant" configuration section.
/// Every value has a working default, so the section only needs the ones you want to change.
public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    /// The kill switch. False = no chat client, no session, no panel. The admin pages still work.
    public bool Enabled { get; set; }

    /// Maximum length of one reply, in tokens.
    public int MaxOutputTokens { get; set; } = 1000;

    /// Low, because this is lookup rather than prose. Null = don't send one (for models that only
    /// accept their default temperature).
    public float? Temperature { get; set; } = 0.2f;

    /// How many rounds of tool calls the model may make while answering one question.
    public int MaxToolIterationsPerTurn { get; set; } = 8;

    /// Maximum cosine distance for a semantic knowledge-base hit to count. Tune per embedding model.
    public double SemanticSearchMaxDistance { get; set; } = 0.6;

    /// Sliding window of messages sent to the model; older ones are dropped.
    public int MaxHistoryMessages { get; set; } = 20;

    /// Longer input is truncated server-side, and the textarea enforces the same limit.
    public int MaxUserInputLength { get; set; } = 2000;

    /// Per turn. The request is cancelled when it runs longer than this.
    public int RequestTimeoutSeconds { get; set; } = 90;

    /// One extra cheap model call per answer, to offer follow-up question chips.
    public bool SuggestFollowUps { get; set; } = true;

    /// Kill switch for the assistant's notebook (its own working notes).
    public bool NotebookEnabled { get; set; } = true;

    /// Notes go into every system prompt; this caps what they cost.
    public int NotebookMaxCharacters { get; set; } = 2000;

    /// Stops one eager conversation flooding the notebook.
    public int NotebookMaxNotesPerConversation { get; set; } = 3;

    /// Tidy-up archives agent notes never reinforced within this many days. 0 disables it.
    public int NotebookStaleAfterDays { get; set; } = 90;

    /// Telemetry only: whether message text reaches the log sink. Feedback and moderator-question
    /// records always keep their conversation snapshot regardless.
    public bool LogConversationContent { get; set; }
}
