using DemoReleaseChatPlugin.Assistant.Prompts;
using DoverPort.Common.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DemoReleaseChatPlugin.Assistant.Chat;

/// One conversation with the assistant. Circuit-scoped (one per browser tab) and not thread-safe —
/// the panel drives it one turn at a time.
///
/// Keeps two parallel lists:
///   _history    — what the MODEL sees: ChatMessages, trimmed to a sliding window.
///   _transcript — what the USER sees: TranscriptEntries, never trimmed, and it keeps failed turns.
public class AssistantChatSession(
    IChatClient chatClient,
    SystemPromptBuilder promptBuilder,
    IOptions<AssistantOptions> options,
    IClock clock,
    ILogger<AssistantChatSession> logger)
{
    private readonly AssistantOptions _options = options.Value;

    private readonly List<ChatMessage> _history = new();
    private readonly List<TranscriptEntry> _transcript = new();

    private string _conversationId = Guid.NewGuid().ToString("N");
    private int _turnNumber;

    public IReadOnlyList<TranscriptEntry> Transcript => _transcript;
    public bool IsBusy { get; private set; }

    /// Raised whenever the transcript or IsBusy changes; the panel subscribes and re-renders.
    public event Action Changed;

    public async Task SendAsync(string userText, CancellationToken cancellationToken = default)
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

        // Added empty now so the panel can show it as "in progress"; filled in when the reply arrives.
        var assistantEntry = NewEntry(TranscriptRole.Assistant, string.Empty);
        _transcript.Add(assistantEntry);

        IsBusy = true;
        NotifyChanged();

        _history.Add(new ChatMessage(ChatRole.User, userText));
        TrimHistory();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));

            // The system prompt is rebuilt every turn and never stored in the history.
            var messages = new List<ChatMessage> { new(ChatRole.System, promptBuilder.Build()) };
            messages.AddRange(_history);

            var chatOptions = new ChatOptions
            {
                MaxOutputTokens = _options.MaxOutputTokens,
                Temperature = _options.Temperature
            };

            var response = await chatClient.GetResponseAsync(messages, chatOptions, cts.Token);

            _history.AddMessages(response);
            assistantEntry.Text = response.Text;

            if (string.IsNullOrWhiteSpace(assistantEntry.Text))
            {
                assistantEntry.IsError = true;
                assistantEntry.Text = "I could not produce an answer this time. Please try rephrasing the question.";
            }
        }
        catch (OperationCanceledException)
        {
            assistantEntry.IsError = true;
            assistantEntry.Text = "The request was cancelled or timed out.";
            ForgetLastUserMessage();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Assistant turn {TurnNumber} of conversation {ConversationId} failed", _turnNumber, _conversationId);

            assistantEntry.IsError = true;
            assistantEntry.Text = "Something went wrong while contacting the assistant service. Please try again.";
            ForgetLastUserMessage();
        }
        finally
        {
            IsBusy = false;
            NotifyChanged();
        }
    }

    private TranscriptEntry NewEntry(TranscriptRole role, string text)
        => new()
        {
            Role = role,
            Text = text,
            Timestamp = clock.Now,
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
        // assistant message has been trimmed away. (No tools yet, but this keeps it right when they come.)
        while (_history.Count > 0 && _history[0].Role != ChatRole.User)
        {
            _history.RemoveAt(0);
        }
    }

    /// The failed turn stays visible in the transcript (the user should see what happened) but leaves
    /// no trace in the history (the model should not see a question it never answered).
    private void ForgetLastUserMessage()
    {
        if (_history.Count > 0 && _history[^1].Role == ChatRole.User)
        {
            _history.RemoveAt(_history.Count - 1);
        }
    }

    private void NotifyChanged() => Changed?.Invoke();
}
