using System;
using System.Collections.Generic;
using DemoReleaseChatPlugin.Assistant.Canvas;
using DemoReleaseChatPlugin.Website.Services.Assistant;
using Microsoft.AspNetCore.Components;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Assistant
{
    /// Shows the report the assistant built. It reads only this circuit's AssistantWorkspaceState, so
    /// everyone sees just their own conversation's results, and it fetches no data of its own.
    public class AssistantWorkspaceBase : AppComponent, IDisposable
    {
        [Inject] protected AssistantWorkspaceState Workspace { get; set; }

        protected IReadOnlyList<AssistantPresentation> Components => Workspace.Current;
        protected bool HasContent => Components.Count > 0;
        protected DateTimeOffset? GeneratedAt => Workspace.GeneratedAt;

        protected override void OnInitialized() => Workspace.Changed += OnWorkspaceChanged;

        protected static bool IsTable(AssistantPresentation component)
            => component.Kind == PresentationKind.Table && component.Table is not null;

        /// Back to the screen the user came from (or home), keeping the report — reports are cleared
        /// only when the conversation is.
        protected void GoBack()
            => Navigation.NavigateTo(string.IsNullOrEmpty(Workspace.ReturnUri) ? "/" : Workspace.ReturnUri);

        // The report grows from the tool loop, which may be off the render thread, so marshal.
        private void OnWorkspaceChanged() => _ = InvokeAsync(StateHasChanged);

        public void Dispose() => Workspace.Changed -= OnWorkspaceChanged;
    }
}
