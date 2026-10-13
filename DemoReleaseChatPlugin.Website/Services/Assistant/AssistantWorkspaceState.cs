using System;
using System.Collections.Generic;
using System.Linq;
using DemoReleaseChatPlugin.Assistant.Canvas;

namespace DemoReleaseChatPlugin.Website.Services.Assistant
{
    /// The workspace report the presentation tools build. Circuit-scoped, registered as itself (for the
    /// panel and the workspace page) and as IAssistantWorkspace (for the tools).
    ///
    /// Current is what the page displays; Latest is the newest report. They differ only when an older
    /// report is re-opened from the transcript — which must not trigger the auto-open again.
    public class AssistantWorkspaceState : IAssistantWorkspace
    {
        /// The page's route, without the leading slash.
        public const string Route = "assistant/workspace";

        private static readonly IReadOnlyList<AssistantPresentation> Empty = Array.Empty<AssistantPresentation>();

        private readonly List<AssistantPresentation> _building = new();
        private bool _startFresh;

        public IReadOnlyList<AssistantPresentation> Current { get; private set; } = Empty;
        public IReadOnlyList<AssistantPresentation> Latest { get; private set; } = Empty;
        public DateTimeOffset? GeneratedAt { get; private set; }

        /// Where the Back button goes: the screen the user was on when the workspace opened.
        public string ReturnUri { get; set; }

        /// Raised from the tool loop, which may be off the render thread — subscribers must marshal.
        public event Action Changed;

        /// Lazy: an answer that calls this and then presents nothing leaves the previous report alone.
        public void StartNewReport() => _startFresh = true;

        public void Show(AssistantPresentation presentation)
        {
            if (_startFresh)
            {
                _building.Clear();
                _startFresh = false;
            }

            _building.Add(presentation);

            // A new list per Show, so a transcript entry holding an earlier snapshot never sees it change.
            var snapshot = _building.ToList();
            Current = snapshot;
            Latest = snapshot;
            GeneratedAt = presentation.CreatedAt;
            Changed?.Invoke();
        }

        /// Shows an earlier report (re-opened from the transcript) without changing which is latest.
        public void Display(IReadOnlyList<AssistantPresentation> components)
        {
            Current = components ?? Empty;
            GeneratedAt = Current.Count > 0 ? Current[^1].CreatedAt : null;
            Changed?.Invoke();
        }

        /// With the conversation: a stale report from a cleared chat must not linger.
        public void Clear()
        {
            _building.Clear();
            _startFresh = false;
            Current = Empty;
            Latest = Empty;
            GeneratedAt = null;
            Changed?.Invoke();
        }
    }
}
