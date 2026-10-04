using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DemoReleaseChatPlugin.Application.Releases;
using DemoReleaseChatPlugin.Assistant;
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
        [Inject] protected ICurrentUserProvider CurrentUser { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }

        [CascadingParameter] protected Task<AuthenticationState> AuthenticationState { get; set; }

        protected AssistantChatSession Session { get; private set; }
        protected string Input { get; set; }
        protected ElementReference InputRef;

        private bool _wasOpen;

        protected bool IsEnabled => Options.Value.Enabled;
        protected int MaxInputLength => Options.Value.MaxUserInputLength;
        protected bool CanSend => Session is { IsBusy: false } && !string.IsNullOrWhiteSpace(Input);

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

            var actor = await CurrentUser.ProvideAsync();
            var authState = await AuthenticationState;
            var roles = authState.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Session.CurrentUser = new AssistantUser(actor.UserId, actor.DisplayName, roles);
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

            await Session.SendAsync(text);
        }

        /// Enter sends, Shift+Enter is a newline. assistant-window.js stops Enter inserting a newline.
        protected async Task OnInputKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter" && !args.ShiftKey)
            {
                await SendAsync();
            }
        }

        protected void Clear() => Session.Clear();

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
            }

            PanelState.Changed -= OnStateChanged;
        }
    }
}
