namespace DemoReleaseChatPlugin.Assistant.Canvas;

/// Where the presentation tools send what they render. Implemented by the host (the Website's
/// AssistantWorkspaceState), which owns the page that displays it.
///
/// The workspace holds ONE report that persists and grows across answers: each Show adds to it.
public interface IAssistantWorkspace
{
    /// Adds a component to the current report.
    void Show(AssistantPresentation presentation);

    /// Begins a fresh report — the next Show starts it clean.
    void StartNewReport();
}
