using System;

namespace DemoReleaseChatPlugin.Website.Services.Assistant
{
    /// Whether the assistant panel is open. Circuit-scoped and registered even when the assistant is
    /// switched off, because the header button and the panel inject it either way.
    public class AssistantPanelState
    {
        public bool IsOpen { get; private set; }

        public event Action Changed;

        public void Toggle() => SetOpen(!IsOpen);

        public void Close() => SetOpen(false);

        private void SetOpen(bool open)
        {
            if (IsOpen == open)
            {
                return;
            }

            IsOpen = open;
            Changed?.Invoke();
        }
    }
}
