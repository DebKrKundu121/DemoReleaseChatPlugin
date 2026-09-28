using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blazored.FluentValidation;
using DemoReleaseChatPlugin.Website.Components.Shared;
using Mediator;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace DemoReleaseChatPlugin.Website.Components
{
    /// <summary>
    /// The base class every page's code-behind inherits.
    ///
    /// Pages drive ALL data access through Mediator.Send(...). Components never touch providers or
    /// the database directly.
    /// </summary>
    public class AppComponent : ComponentBase
    {
        [Inject] protected IMediator Mediator { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected TooltipService TooltipService { get; set; }
        [Inject] protected NotificationService Notification { get; set; }

        /// <summary>
        /// Client-side validation, run before Mediator.Send so the user sees failures marked against
        /// the fields they belong to.
        /// <para>
        /// It is no longer load-bearing for correctness: the mediator pipeline now short-circuits
        /// with a FAILED RESULT rather than throwing, so a form that posts an invalid command gets a
        /// message on the Result instead of an unhandled exception. This stays because per-field
        /// messages are a better experience than one combined sentence in a dialog - it is a UX
        /// choice now, not a crash guard.
        /// </para>
        /// </summary>
        protected static async Task<bool> IsValidAsync(FluentValidationValidator validator) =>
            validator is not null && await validator.ValidateAsync();

        protected async Task ViewHistory(Guid correlationId)
        {
            await DialogService.OpenAsync<History>(
                "History",
                new Dictionary<string, object> { [nameof(History.CorrelationId)] = correlationId },
                new DialogOptions { Width = "640px" });
        }
    }
}
