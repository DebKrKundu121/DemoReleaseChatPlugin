using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Authorization;
using DemoReleaseChatPlugin.Application.Releases;
using DemoReleaseChatPlugin.Assistant;
using DemoReleaseChatPlugin.Assistant.Canvas;
using DemoReleaseChatPlugin.Assistant.Chat;
using DemoReleaseChatPlugin.Website.Services.Assistant;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace DemoReleaseChatPlugin.Website.Components.Shared.Assistant
{
    public class AssistantPanelBase : ComponentBase, IDisposable
    {
        protected const string PanelElementId = "assistant-panel";

        [Inject] protected IServiceProvider ServiceProvider { get; set; }
        [Inject] protected IOptions<AssistantOptions> Options { get; set; }
        [Inject] protected AssistantPanelState PanelState { get; set; }
        [Inject] protected AssistantWorkspaceState Workspace { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }
        [Inject] protected ICurrentUserProvider CurrentUser { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }

        [CascadingParameter] protected Task<AuthenticationState> AuthenticationState { get; set; }

        protected AssistantChatSession Session { get; private set; }
        protected string Input { get; set; }
        protected ElementReference InputRef;

        private bool _wasOpen;
        private IReadOnlyList<AssistantPresentation> _lastReport;
        private AssistantCapabilities _capabilities = AssistantCapabilities.None;

        protected bool IsEnabled => Options.Value.Enabled;
        protected int MaxInputLength => Options.Value.MaxUserInputLength;
        protected bool CanSend => Session is { IsBusy: false } && !string.IsNullOrWhiteSpace(Input);

        /// Busy, and the reply has no words yet: show what the assistant is doing instead.
        protected bool IsWaitingForText => Session is { IsBusy: true }
                                           && Session.Transcript.Count > 0
                                           && string.IsNullOrEmpty(Session.Transcript[^1].Text);

        protected override async Task OnInitializedAsync()
        {
            if (!IsEnabled)
            {
                return;
            }

            // The session is only registered when the assistant is enabled, so it is resolved here
            // rather than [Inject]-ed — an [Inject] would throw on a disabled app.
            Session = ServiceProvider.GetRequiredService<AssistantChatSession>();

            Session.Changed += OnStateChanged;
            PanelState.Changed += OnStateChanged;
            Workspace.Changed += OnWorkspaceChanged;

            var actor = await CurrentUser.ProvideAsync();
            var authState = await AuthenticationState;
            var roles = authState.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Session.CurrentUser = new AssistantUser(actor.UserId, actor.DisplayName, roles);

            // The same rule as the release pages: IT Admins only.
            _capabilities = new AssistantCapabilities(
                CanAccessReleases: authState.User.IsInRole(RoleConstants.ItAdmin));
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            var open = IsEnabled && PanelState.IsOpen && Session != null;

            // Only when the panel has just opened — not on every streamed re-render.
            if (open && !_wasOpen)
            {
                await JS.InvokeVoidAsync("assistantWindow.bindEnterToSend", InputRef);
                await InputRef.FocusAsync();
            }

            _wasOpen = open;
        }

        protected async Task SendAsync()
        {
            if (!CanSend)
            {
                return;
            }

            var text = Input;
            Input = string.Empty;          // cleared before the await, so the box empties immediately

            await Session.SendAsync(text, _capabilities);
        }

        /// Enter sends, Shift+Enter is a newline. assistant-window.js stops Enter inserting a newline.
        protected async Task OnInputKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter" && !args.ShiftKey)
            {
                await SendAsync();
            }
        }

        /// One per kind of question, so the user learns the shape of a good one. Only for users who
        /// have the release tools — anyone else would just be told they lack access.
        protected string[] StarterPrompts => _capabilities.CanAccessReleases
            ? new[]
            {
                "List every project as a table",
                "What version is each project on?",
                "Which releases were rolled back this year?"
            }
            : Array.Empty<string>();

        protected async Task SendStarterAsync(string prompt)
        {
            Input = prompt;
            await SendAsync();
        }

        protected void Clear()
        {
            Session.Clear();
            Workspace.Clear();
        }

        /// Re-opens the report an earlier answer produced. Display changes only Current, so this
        /// doesn't count as a new report and won't re-trigger the auto-open.
        protected void OpenReport(IReadOnlyList<AssistantPresentation> components)
        {
            if (components is null || components.Count == 0)
            {
                return;
            }

            Workspace.Display(components);
            OpenWorkspace();
        }

        /// A fresh report opens the workspace ONCE — on its first component. Later components grow the
        /// same report while the user is already looking at it.
        private void OnWorkspaceChanged() => _ = InvokeAsync(() =>
        {
            var latest = Workspace.Latest;

            if (latest.Count > 0 && !ReferenceEquals(latest, _lastReport))
            {
                var firstComponentOfReport = _lastReport is null || _lastReport.Count == 0 || !SameReport(latest, _lastReport);
                _lastReport = latest;
                StampCurrentTurn(latest);

                if (firstComponentOfReport && !IsOnWorkspace)
                {
                    OpenWorkspace();
                }
            }
            else if (latest.Count == 0)
            {
                _lastReport = latest;
            }

            StateHasChanged();
        });

        private void OpenWorkspace()
        {
            if (!IsOnWorkspace)
            {
                Workspace.ReturnUri = Navigation.Uri;
                Navigation.NavigateTo("/" + AssistantWorkspaceState.Route);
            }

            PanelState.Close();      // hide the chat so the report gets the full screen
        }

        /// Links the report to the reply being written, for its "Open workspace" link. Each Show is a
        /// new snapshot, so the last stamp of the turn is the whole report.
        private void StampCurrentTurn(IReadOnlyList<AssistantPresentation> report)
        {
            if (Session is { IsBusy: true, Transcript.Count: > 0 } && Session.Transcript[^1].Role == TranscriptRole.Assistant)
            {
                Session.Transcript[^1].Presentations = report;
            }
        }

        /// Two snapshots are the same report when one is a prefix of the other — a report only ever
        /// grows by appending. So a report built table → table navigates once, not twice.
        private static bool SameReport(IReadOnlyList<AssistantPresentation> a, IReadOnlyList<AssistantPresentation> b)
        {
            var shorter = a.Count < b.Count ? a : b;
            var longer = a.Count < b.Count ? b : a;

            for (var i = 0; i < shorter.Count; i++)
            {
                if (!ReferenceEquals(shorter[i], longer[i]))
                {
                    return false;
                }
            }

            return shorter.Count > 0;
        }

        private bool IsOnWorkspace =>
            Navigation.ToBaseRelativePath(Navigation.Uri).TrimEnd('/')
                .Equals(AssistantWorkspaceState.Route, StringComparison.OrdinalIgnoreCase);

        protected static string RoleClass(TranscriptEntry entry) => entry.Role == TranscriptRole.User
            ? "assistant-message-user"
            : entry.IsError
                ? "assistant-message-assistant assistant-message-error"
                : "assistant-message-assistant";

        // Session and panel events can arrive off the render thread (e.g. mid-stream), so marshal.
        private void OnStateChanged() => _ = InvokeAsync(StateHasChanged);

        public void Dispose()
        {
            if (Session != null)
            {
                Session.Changed -= OnStateChanged;
                Workspace.Changed -= OnWorkspaceChanged;
            }

            PanelState.Changed -= OnStateChanged;
        }
    }
}
