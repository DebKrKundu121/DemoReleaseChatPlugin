using System.Threading.Tasks;
using Blazored.FluentValidation;
using DemoReleaseChatPlugin.Application.Releases.Notices;
using DoverPort.AspNetCore.Common.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components.Pages.Releases
{
    /// Shared by the three notice forms.
    public class ReleaseNoticeFormBase : AppComponent
    {
        [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; }

        protected bool IsSubmitting { get; set; }

        protected FluentValidationValidator Validator { get; set; }

        /// <summary>
        /// Pre-fills "Lead developer" with the signed-in user's display name - a convenience default
        /// the user can overwrite, because someone may file on the lead developer's behalf.
        /// </summary>
        protected async Task<string> ResolveCurrentUserNameAsync()
        {
            if (AuthenticationStateTask is null) return string.Empty;

            var state = await AuthenticationStateTask;

            if (state?.User?.Identity?.IsAuthenticated != true) return string.Empty;

            return state.User.GetDisplayName() ?? string.Empty;
        }

        /// <summary>
        /// Deliberately gives NO recipient count: the handler never looked at the mailing list, and
        /// nothing has been sent when the page responds.
        /// </summary>
        protected void ReportOutcome(FileNoticeResult result, string successSummary)
        {
            Notification.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = successSummary,
                Detail = "Stakeholders will be notified by email shortly.",
                Duration = 5000
            });
        }
    }
}
