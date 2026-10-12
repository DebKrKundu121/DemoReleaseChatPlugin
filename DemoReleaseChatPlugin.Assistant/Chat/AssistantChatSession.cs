using DemoReleaseChatPlugin.Assistant.Prompts;
using DemoReleaseChatPlugin.Assistant.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DemoReleaseChatPlugin.Assistant.Chat;

/// One conversation with the assistant. Circuit-scoped (one per browser tab) and not thread-safe:
/// the panel drives it one turn at a time.
///
/// Keeps two lists: the model's history (trimmed to a sliding window) and the UI's transcript (never
/// trimmed). A failed turn stays visible in the transcript but leaves no trace in the history.
public sealed class AssistantChatSession
{
    private readonly IChatClient _chatClient;
    private readonly SystemPromptBuilder _promptBuilder;
    private readonly AssistantToolRegistry _toolRegistry;
    private readonly AssistantOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AssistantChatSession> _logger;

    private readonly List<ChatMessage> _history = new();
    private readonly List<TranscriptEntry> _transcript = new();

    private string _conversationId = NewConversationId();
    private int _turnNumber;
    private CancellationTokenSource _cts;

    public AssistantChatSession(
        IChatClient chatClient,
        SystemPromptBuilder promptBuilder,
        AssistantToolRegistry toolRegistry,
        IOptions<AssistantOptions> options,
        TimeProvider timeProvider,
        ILogger<AssistantChatSession> logger)
    {
        _chatClient = chatClient;
        _promptBuilder = promptBuilder;
        _toolRegistry = toolRegistry;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public IReadOnlyList<TranscriptEntry> Transcript => _transcript;
    public bool IsBusy { get; private set; }

    /// What the assistant is doing right now, e.g. "Checking the date…". Null once text starts arriving.
    public string CurrentActivity { get; private set; }

    /// Set by the host once the signed-in user is known.
    public AssistantUser CurrentUser { get; set; } = AssistantUser.Unknown;

    /// Raised after every change, including each streamed fragment, so the panel can re-render.
    public event Action Changed;

    /// capabilities decides which tools are offered this turn — see AssistantToolRegistry.
    public async Task SendAsync(string userText, AssistantCapabilities capabilities, CancellationToken cancellationToken = default)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(userText))
        {
            return;
        }

        if (userText.Length > _options.MaxUserInputLength)
        {
            userText = userText[.._options.MaxUserInputLength];
        }

        _turnNumber++;

        _transcript.Add(NewEntry(TranscriptRole.User, userText));
        var assistantEntry = NewEntry(TranscriptRole.Assistant, string.Empty);
        _transcript.Add(assistantEntry);

        IsBusy = true;
        NotifyChanged();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cts.CancelAfter(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));

        _history.Add(new ChatMessage(ChatRole.User, userText));
        TrimHistory();

        try
        {
            capabilities ??= AssistantCapabilities.None;

            var messages = new List<ChatMessage> { new(ChatRole.System, _promptBuilder.Build(CurrentUser, capabilities)) };
            messages.AddRange(_history);

            // The tool loop (UseFunctionInvocation on the chat client) runs whatever the model calls
            // from this list and feeds the results back; this session never dispatches a tool itself.
            var chatOptions = new ChatOptions
            {
                Tools = _toolRegistry.BuildTools(capabilities).ToList(),
                MaxOutputTokens = _options.MaxOutputTokens,
                Temperature = _options.Temperature
            };

            var updates = new List<ChatResponseUpdate>();

            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, chatOptions, _cts.Token))
            {
                updates.Add(update);

                // The tool loop passes the model's tool-call requests through the stream before it
                // runs them, so they can be recorded here as they happen.
                foreach (var call in update.Contents.OfType<FunctionCallContent>())
                {
                    var label = ToolNames.DisplayLabel(call.Name);
                    assistantEntry.ToolActivities.Add(new ToolActivity { ToolName = call.Name, DisplayLabel = label });
                    CurrentActivity = label;
                }

                if (!string.IsNullOrEmpty(update.Text))
                {
                    assistantEntry.Text += update.Text;
                    CurrentActivity = null;         // text has started, so the activity line goes away
                }

                NotifyChanged();
            }

            // Collapses the whole stream back into history in one go. Don't hand-build messages from
            // the deltas — once tools exist, this is what keeps tool calls and results paired.
            _history.AddMessages(updates.ToChatResponse());

            if (string.IsNullOrWhiteSpace(assistantEntry.Text))
            {
                assistantEntry.IsError = true;
                assistantEntry.Text = "I could not produce an answer this time. Please try rephrasing the question.";
            }
        }
        catch (OperationCanceledException)
        {
            if (string.IsNullOrWhiteSpace(assistantEntry.Text))
            {
                assistantEntry.IsError = true;
                assistantEntry.Text = "The request was cancelled or timed out.";
            }
            else
            {
                assistantEntry.Text += " *(cancelled)*";
            }

            ForgetLastUserMessage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assistant turn {TurnNumber} of conversation {ConversationId} failed", _turnNumber, _conversationId);

            assistantEntry.IsError = true;
            assistantEntry.Text = "Something went wrong while contacting the assistant service. Please try again.";

            ForgetLastUserMessage();
        }
        finally
        {
            IsBusy = false;
            CurrentActivity = null;
            _cts.Dispose();
            _cts = null;
            NotifyChanged();
        }
    }

    public void Cancel() => _cts?.Cancel();

    public void Clear()
    {
        Cancel();
        _history.Clear();
        _transcript.Clear();
        _conversationId = NewConversationId();
        _turnNumber = 0;
        NotifyChanged();
    }

    private TranscriptEntry NewEntry(TranscriptRole role, string text) => new()
    {
        Role = role,
        Text = text,
        Timestamp = _timeProvider.GetLocalNow(),
        ConversationId = _conversationId,
        TurnNumber = _turnNumber
    };

    private void TrimHistory()
    {
        while (_history.Count > _options.MaxHistoryMessages)
        {
            _history.RemoveAt(0);
        }

        // Never let the window start mid tool-exchange: the API rejects tool results whose calling
        // assistant message has been trimmed away.
        while (_history.Count > 0 && _history[0].Role != ChatRole.User)
        {
            _history.RemoveAt(0);
        }
    }

    /// The failed turn never produced a valid reply; drop the dangling user message so the next send
    /// starts from a consistent history.
    private void ForgetLastUserMessage()
    {
        if (_history.Count > 0 && _history[^1].Role == ChatRole.User)
        {
            _history.RemoveAt(_history.Count - 1);
        }
    }

    private void NotifyChanged() => Changed?.Invoke();

    private static string NewConversationId() => Guid.NewGuid().ToString("N");
}
